using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace PTL.Framework.Services
{
    /// <summary>
    /// <see cref="IStoreGateway"/> over Unity IAP v5. This is the only file in the game that references
    /// <c>UnityEngine.Purchasing</c>. Events are subscribed before connecting because purchases left
    /// pending by an earlier session can fire as soon as the store connects.
    /// </summary>
    public sealed class UnityIapStoreGateway : IStoreGateway
    {
        private readonly List<ProductDefinition> _definitions = new();
        private StoreController _store;

        public event Action OnReady;
        public event Action<string> OnUnavailable;
        public event Action<StoreTransaction> OnPurchasePending;
        public event Action<string> OnEntitlementFound;
        public event Action<string, StoreFailureKind, string> OnPurchaseFailed;
        public event Action<string> OnPurchaseDeferred;

        public bool IsReady { get; private set; }

        public async void Connect(IReadOnlyList<StoreProductRequest> products)
        {
            _definitions.Clear();
            foreach (StoreProductRequest product in products)
                _definitions.Add(new ProductDefinition(product.ProductId,
                    product.Consumable ? ProductType.Consumable : ProductType.NonConsumable));

            _store = UnityIAPServices.StoreController();

            _store.OnStoreConnected += HandleStoreConnected;
            _store.OnStoreDisconnected += HandleStoreDisconnected;
            _store.OnProductsFetched += HandleProductsFetched;
            _store.OnProductsFetchFailed += HandleProductsFetchFailed;
            _store.OnPurchasesFetched += HandlePurchasesFetched;
            _store.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
            _store.OnPurchasePending += HandlePurchasePending;
            _store.OnPurchaseConfirmed += HandlePurchaseConfirmed;
            _store.OnPurchaseFailed += HandlePurchaseFailed;
            _store.OnPurchaseDeferred += HandlePurchaseDeferred;
            _store.OnAuthAccountChanged += HandleAccountChanged;

            try
            {
                await _store.Connect();
            }
            catch (Exception e)
            {
                MarkUnavailable($"Connect threw: {e.Message}");
            }
        }

        public void Disconnect()
        {
            if (_store == null) return;

            _store.OnStoreConnected -= HandleStoreConnected;
            _store.OnStoreDisconnected -= HandleStoreDisconnected;
            _store.OnProductsFetched -= HandleProductsFetched;
            _store.OnProductsFetchFailed -= HandleProductsFetchFailed;
            _store.OnPurchasesFetched -= HandlePurchasesFetched;
            _store.OnPurchasesFetchFailed -= HandlePurchasesFetchFailed;
            _store.OnPurchasePending -= HandlePurchasePending;
            _store.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
            _store.OnPurchaseFailed -= HandlePurchaseFailed;
            _store.OnPurchaseDeferred -= HandlePurchaseDeferred;
            _store.OnAuthAccountChanged -= HandleAccountChanged;
            _store = null;
            IsReady = false;
        }

        public string GetLocalizedPrice(string productId)
        {
            Product product = _store?.GetProductById(productId);
            string price = product?.metadata?.localizedPriceString;
            return string.IsNullOrEmpty(price) ? null : price;
        }

        public void Purchase(string productId)
        {
            Product product = _store?.GetProductById(productId);
            if (product == null || !product.availableToPurchase)
            {
                OnPurchaseFailed?.Invoke(productId, StoreFailureKind.Unavailable, "Product not available from the store.");
                return;
            }

            _store.PurchaseProduct(product);
        }

        public void Confirm(StoreTransaction transaction)
        {
            if (_store != null && transaction.Handle is PendingOrder order)
                _store.ConfirmPurchase(order);
        }

        public void Restore(Action<bool> onComplete)
        {
            if (_store == null)
            {
                onComplete?.Invoke(false);
                return;
            }

            _store.RestoreTransactions((success, error) =>
            {
                if (success) _store?.FetchPurchases();
                else Debug.LogWarning($"[UnityIapStoreGateway] Restore failed: {error}");
                onComplete?.Invoke(success);
            });
        }

#region Store callbacks

        private void HandleStoreConnected() => _store.FetchProducts(_definitions);

        private void HandleStoreDisconnected(StoreConnectionFailureDescription failure)
            => MarkUnavailable(failure.Message);

        private void HandleProductsFetched(List<Product> products)
        {
            IsReady = true;
            OnReady?.Invoke();
            _store.FetchPurchases();
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failure)
            => MarkUnavailable($"Product fetch failed: {failure.FailureReason}");

        private void HandlePurchasesFetched(Orders orders)
        {
            foreach (ConfirmedOrder order in orders.ConfirmedOrders)
                OnEntitlementFound?.Invoke(ProductIdOf(order));
        }

        private static void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
            => Debug.LogWarning($"[UnityIapStoreGateway] Purchase fetch failed: {failure.Message}");

        private void HandlePurchasePending(PendingOrder order)
            => OnPurchasePending?.Invoke(new StoreTransaction(ProductIdOf(order), order.Info.TransactionID, order.Info.Receipt, order));

        private static void HandlePurchaseConfirmed(Order order)
        {
            // The grant already happened. A failed confirm leaves the order pending, so it is
            // redelivered next launch, where the transaction ledger stops it granting twice.
            if (order is FailedOrder failed)
                Debug.LogWarning($"[UnityIapStoreGateway] Confirm failed for '{ProductIdOf(order)}': {failed.FailureReason} - {failed.Details}");
        }

        private void HandlePurchaseFailed(FailedOrder order)
            => OnPurchaseFailed?.Invoke(ProductIdOf(order), Classify(order.FailureReason), $"{order.FailureReason}: {order.Details}");

        private void HandlePurchaseDeferred(DeferredOrder order) => OnPurchaseDeferred?.Invoke(ProductIdOf(order));

        /// <summary>IAP clears its product and purchase caches before raising this, so load everything again.</summary>
        private void HandleAccountChanged()
        {
            IsReady = false;
            _store.FetchProducts(_definitions);
        }

#endregion

        private void MarkUnavailable(string message)
        {
            IsReady = false;
            OnUnavailable?.Invoke(message);
        }

        private static StoreFailureKind Classify(PurchaseFailureReason reason)
        {
            switch (reason)
            {
                case PurchaseFailureReason.UserCancelled:
                    return StoreFailureKind.Cancelled;

                case PurchaseFailureReason.PurchasingUnavailable:
                case PurchaseFailureReason.ProductUnavailable:
                case PurchaseFailureReason.StoreNotConnected:
                    return StoreFailureKind.Unavailable;

                default:
                    return StoreFailureKind.Failed;
            }
        }

        private static string ProductIdOf(Order order)
        {
            IReadOnlyList<CartItem> items = order.CartOrdered?.Items();
            return items != null && items.Count > 0 ? items[0].Product?.definition?.id : null;
        }
    }
}
