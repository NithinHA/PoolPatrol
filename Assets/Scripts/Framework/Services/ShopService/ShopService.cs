using System;
using System.Collections.Generic;
using Loadout;
using Monetization;
using UnityEngine;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Default <see cref="IShopService"/>. Builds one product table from the shop and loadout catalogs,
    /// loads it into the gateway, and fulfils payments.
    ///
    /// Fulfilment order: validate, record the transaction, grant, flush PlayerPrefs, then confirm. The
    /// ledger and the grant reach disk in the same flush, and an unconfirmed payment is redelivered, so a
    /// crash can't lose a purchase and a redelivery can't grant a consumable twice.
    /// </summary>
    public class ShopService : IShopService
    {
        /// <summary>What a store product grants.</summary>
        private class ProductEntry
        {
            public string DisplayName;
            public bool Consumable;
            public string FallbackPrice;
            public int Coins;
            public IReadOnlyList<LoadoutItemSO> Items;
        }

        private const string KeyLedgerPrefix = "Shop_Txn_";

        private readonly ShopCatalogSO _catalog;
        private readonly LoadoutCatalogSO _loadoutCatalog;
        private readonly IStoreGateway _gateway;
        private readonly IReceiptValidator _validator;
        private readonly Dictionary<string, ProductEntry> _products = new();
        private string _inFlightProductId;

        public event Action OnStoreStateChanged;
        public event Action<ShopResult> OnPurchaseResolved;

        public ShopService(ShopCatalogSO catalog, LoadoutCatalogSO loadoutCatalog, IStoreGateway gateway, IReceiptValidator validator)
        {
            if (catalog == null)
            {
                Debug.LogError("[ShopService] No ShopCatalog assigned on Bootstrap — the Bundles tab will be empty.");
                catalog = ScriptableObject.CreateInstance<ShopCatalogSO>();
            }

            _catalog = catalog;
            _loadoutCatalog = loadoutCatalog;
            _gateway = gateway;
            _validator = validator;
        }

#region Default callbacks

        public void Start()
        {
            BuildProductTable();

            _gateway.OnReady += HandleStoreStateChanged;
            _gateway.OnUnavailable += HandleUnavailable;
            _gateway.OnPurchasePending += HandlePurchasePending;
            _gateway.OnEntitlementFound += HandleEntitlementFound;
            _gateway.OnPurchaseFailed += HandlePurchaseFailed;
            _gateway.OnPurchaseDeferred += HandlePurchaseDeferred;

            var requests = new List<StoreProductRequest>(_products.Count);
            foreach (KeyValuePair<string, ProductEntry> pair in _products)
                requests.Add(new StoreProductRequest(pair.Key, pair.Value.Consumable, pair.Value.FallbackPrice));
            _gateway.Connect(requests);
        }

        public void OnDestroy()
        {
            _gateway.OnReady -= HandleStoreStateChanged;
            _gateway.OnUnavailable -= HandleUnavailable;
            _gateway.OnPurchasePending -= HandlePurchasePending;
            _gateway.OnEntitlementFound -= HandleEntitlementFound;
            _gateway.OnPurchaseFailed -= HandlePurchaseFailed;
            _gateway.OnPurchaseDeferred -= HandlePurchaseDeferred;
            _gateway.Disconnect();
        }

#endregion

        public IReadOnlyList<CoinPackSO> CoinPacks => _catalog.CoinPacks;
        public IReadOnlyList<ItemBundleSO> Bundles => _catalog.Bundles;

        public bool CanStartPurchase => _gateway.IsReady && _inFlightProductId == null;

        public string GetPrice(string productId)
        {
            string price = _gateway.GetLocalizedPrice(productId);
            if (!string.IsNullOrEmpty(price)) return price;
            return _products.TryGetValue(productId ?? string.Empty, out ProductEntry entry) ? entry.FallbackPrice : string.Empty;
        }

        public bool IsOwned(ItemBundleSO bundle)
        {
            ILoadoutService loadout = ServiceLocator.GetLoadoutService();
            if (loadout == null || bundle == null || bundle.Items.Count == 0) return false;

            foreach (LoadoutItemSO item in bundle.Items)
                if (!loadout.IsUnlocked(item)) return false;
            return true;
        }

#region Buying

        public void BuyWithCoins(LoadoutItemSO item)
        {
            string displayName = item != null ? item.DisplayName : string.Empty;
            ILoadoutService loadout = ServiceLocator.GetLoadoutService();
            PurchaseResult result = loadout?.TryPurchase(item) ?? PurchaseResult.Unavailable;

            switch (result)
            {
                case PurchaseResult.Success:
                case PurchaseResult.AlreadyUnlocked:
                    Resolve(new ShopResult(ShopOutcome.Success, displayName));
                    break;

                case PurchaseResult.InsufficientCoins:
                    int balance = ServiceLocator.GetEconomyService()?.GetBalance(CurrencyType.Coins) ?? 0;
                    Resolve(new ShopResult(ShopOutcome.InsufficientCoins, displayName, item.CoinPrice - balance));
                    break;

                case PurchaseResult.Unavailable:
                    Resolve(new ShopResult(ShopOutcome.Unavailable, displayName));
                    break;

                default:    // NotForSale: a coin button on an item with no coin price is a data mistake.
                    Debug.LogError($"[ShopService] '{displayName}' is not a coin item.");
                    Resolve(new ShopResult(ShopOutcome.Failed, displayName));
                    break;
            }
        }

        public void BuyProduct(string productId)
        {
            string displayName = DisplayNameOf(productId);

            if (!_products.ContainsKey(productId ?? string.Empty))
            {
                Debug.LogError($"[ShopService] Unknown product '{productId}'.");
                Resolve(new ShopResult(ShopOutcome.Failed, displayName));
                return;
            }
            if (!CanStartPurchase)
            {
                Resolve(new ShopResult(ShopOutcome.Unavailable, displayName));
                return;
            }

            _inFlightProductId = productId;
            OnStoreStateChanged?.Invoke();
            _gateway.Purchase(productId);
        }

        public void RestorePurchases(Action<bool> onComplete) => _gateway.Restore(onComplete);

#endregion

#region Gateway callbacks

        private void HandleStoreStateChanged() => OnStoreStateChanged?.Invoke();

        private void HandleUnavailable(string message)
        {
            Debug.LogWarning($"[ShopService] Store unavailable: {message}");
            OnStoreStateChanged?.Invoke();
        }

        private void HandlePurchasePending(StoreTransaction transaction)
        {
            EndInFlight(transaction.ProductId);

            if (!_products.TryGetValue(transaction.ProductId ?? string.Empty, out ProductEntry entry))
            {
                // Left unconfirmed on purpose: a later build that knows this product can still fulfil it.
                Debug.LogError($"[ShopService] Payment for unknown product '{transaction.ProductId}' — not confirmed.");
                Resolve(new ShopResult(ShopOutcome.Failed, transaction.ProductId));
                return;
            }

            if (!_validator.IsValid(transaction))
            {
                Debug.LogError($"[ShopService] Receipt rejected for '{transaction.ProductId}' — nothing granted.");
                Resolve(new ShopResult(ShopOutcome.Failed, entry.DisplayName));
                return;
            }

            if (!IsRecorded(transaction.TransactionId))
            {
                Record(transaction.TransactionId);
                Grant(entry);
                PlayerPrefs.Save();
            }

            _gateway.Confirm(transaction);
            Resolve(new ShopResult(ShopOutcome.Success, entry.DisplayName));
        }

        /// <summary>Only ever reports one-time purchases, and granting those twice is harmless.</summary>
        private void HandleEntitlementFound(string productId)
        {
            if (productId != null && _products.TryGetValue(productId, out ProductEntry entry) && !entry.Consumable)
                Grant(entry);
        }

        private void HandlePurchaseFailed(string productId, StoreFailureKind kind, string message)
        {
            string displayName = DisplayNameOf(productId ?? _inFlightProductId);
            EndInFlight(productId);

            switch (kind)
            {
                case StoreFailureKind.Cancelled:
                    Resolve(new ShopResult(ShopOutcome.Cancelled, displayName));
                    break;

                case StoreFailureKind.Unavailable:
                    Debug.LogWarning($"[ShopService] '{productId}' unavailable: {message}");
                    Resolve(new ShopResult(ShopOutcome.Unavailable, displayName));
                    break;

                default:
                    Debug.LogWarning($"[ShopService] '{productId}' failed: {message}");
                    Resolve(new ShopResult(ShopOutcome.Failed, displayName));
                    break;
            }
        }

        private void HandlePurchaseDeferred(string productId)
        {
            string displayName = DisplayNameOf(productId ?? _inFlightProductId);
            EndInFlight(productId);
            Resolve(new ShopResult(ShopOutcome.Deferred, displayName));
        }

#endregion

        /// <summary>
        /// Frees the purchase slot, unless the event is about a different product: a payment redelivered
        /// from an earlier session must not unlock the button while the player's new purchase is still open.
        /// </summary>
        private void EndInFlight(string productId)
        {
            if (productId == null || productId == _inFlightProductId)
                _inFlightProductId = null;
        }

        private void Resolve(ShopResult result)
        {
            OnPurchaseResolved?.Invoke(result);
            OnStoreStateChanged?.Invoke();
        }

        private static void Grant(ProductEntry entry)
        {
            if (entry.Coins > 0)
                ServiceLocator.GetEconomyService()?.Grant(CurrencyType.Coins, entry.Coins);

            if (entry.Items == null) return;

            ILoadoutService loadout = ServiceLocator.GetLoadoutService();
            foreach (LoadoutItemSO item in entry.Items)
                loadout?.GrantOwnership(item);
        }

        private static bool IsRecorded(string transactionId)
            => !string.IsNullOrEmpty(transactionId) && PlayerPrefs.GetInt(KeyLedgerPrefix + transactionId, 0) == 1;

        private static void Record(string transactionId)
        {
            if (!string.IsNullOrEmpty(transactionId))
                PlayerPrefs.SetInt(KeyLedgerPrefix + transactionId, 1);
        }

        private string DisplayNameOf(string productId)
            => productId != null && _products.TryGetValue(productId, out ProductEntry entry) ? entry.DisplayName : string.Empty;

#region Product table

        private void BuildProductTable()
        {
            foreach (CoinPackSO pack in _catalog.CoinPacks)
                if (pack != null)
                    AddProduct(pack.ProductId, new ProductEntry
                    {
                        DisplayName = pack.DisplayName, Consumable = pack.IsConsumable,
                        FallbackPrice = pack.FallbackPrice, Coins = pack.Coins,
                    });

            foreach (ItemBundleSO bundle in _catalog.Bundles)
                if (bundle != null)
                    AddProduct(bundle.ProductId, new ProductEntry
                    {
                        DisplayName = bundle.DisplayName, Consumable = bundle.IsConsumable,
                        FallbackPrice = bundle.FallbackPrice, Items = bundle.Items,
                    });

            if (_loadoutCatalog == null) return;

            AddItemProducts(_loadoutCatalog.Characters);
            AddItemProducts(_loadoutCatalog.Floaties);
            AddItemProducts(_loadoutCatalog.Weapons);
        }

        private void AddItemProducts<T>(List<T> items) where T : LoadoutItemSO
        {
            foreach (T item in items)
                if (item != null && item.Unlock == UnlockType.IAP)
                    AddProduct(item.IapProductId, new ProductEntry
                    {
                        DisplayName = item.DisplayName, Consumable = false,
                        FallbackPrice = item.IapFallbackPrice, Items = new LoadoutItemSO[] { item },
                    });
        }

        private void AddProduct(string productId, ProductEntry entry)
        {
            if (string.IsNullOrEmpty(productId))
                Debug.LogError($"[ShopService] '{entry.DisplayName}' has no store product id — it can't be sold.");
            else if (!_products.TryAdd(productId, entry))
                Debug.LogError($"[ShopService] Product id '{productId}' is used by both '{_products[productId].DisplayName}' and '{entry.DisplayName}'.");
        }

#endregion
    }
}
