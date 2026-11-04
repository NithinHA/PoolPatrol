using System;
using System.Collections.Generic;
using Loadout;
using Monetization;

namespace PTL.Framework.Services
{
    public enum ShopOutcome
    {
        Success,
        /// <summary>The player backed out of the store's dialog.</summary>
        Cancelled,
        /// <summary>Awaiting approval (Ask to Buy, deferred payment). Nothing granted yet.</summary>
        Deferred,
        InsufficientCoins,
        /// <summary>The store can't be reached, or another purchase is already in progress.</summary>
        Unavailable,
        Failed,
    }

    /// <summary>What happened to a purchase attempt, for the UI to report.</summary>
    public readonly struct ShopResult
    {
        public readonly ShopOutcome Outcome;
        public readonly string DisplayName;

        /// <summary>Coins the player was short by, for <see cref="ShopOutcome.InsufficientCoins"/>.</summary>
        public readonly int Shortfall;

        public ShopResult(ShopOutcome outcome, string displayName, int shortfall = 0)
        {
            Outcome = outcome;
            DisplayName = displayName;
            Shortfall = shortfall;
        }
    }

    /// <summary>
    /// The main-menu Shop's single entry point for buying things: coin purchases through the loadout and
    /// economy services, and real-money purchases through an <see cref="IStoreGateway"/>. UI never touches
    /// a store API, and fulfilment (coins, item ownership) happens here whichever way the money came in.
    /// Registered in <see cref="Bootstrap"/>, after the services it grants through.
    /// </summary>
    public interface IShopService : IService
    {
        IReadOnlyList<CoinPackSO> CoinPacks { get; }
        IReadOnlyList<ItemBundleSO> Bundles { get; }

        /// <summary>The store connected or dropped, or a purchase started or finished: re-read prices and availability.</summary>
        event Action OnStoreStateChanged;

        /// <summary>Fired once for every purchase attempt, coin or real money, when it resolves.</summary>
        event Action<ShopResult> OnPurchaseResolved;

        /// <summary>Real-money purchases can start right now (store ready, none already running).</summary>
        bool CanStartPurchase { get; }

        /// <summary>The store's localized price, falling back to the authored one.</summary>
        string GetPrice(string productId);

        /// <summary>True when every item in the bundle is already unlocked.</summary>
        bool IsOwned(ItemBundleSO bundle);

        /// <summary>Spends coins on an item. Resolves immediately through <see cref="OnPurchaseResolved"/>.</summary>
        void BuyWithCoins(LoadoutItemSO item);

        /// <summary>Starts a real-money purchase. Resolves later through <see cref="OnPurchaseResolved"/>.</summary>
        void BuyProduct(string productId);

        /// <summary>Asks the store to redeliver one-time purchases (required on iOS). Calls back with success.</summary>
        void RestorePurchases(Action<bool> onComplete);
    }
}
