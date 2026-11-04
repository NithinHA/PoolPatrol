using System;
using System.Collections.Generic;

namespace PTL.Framework.Services
{
    /// <summary>A product the gateway should load from the store.</summary>
    public readonly struct StoreProductRequest
    {
        public readonly string ProductId;
        public readonly bool Consumable;
        public readonly string FallbackPrice;

        public StoreProductRequest(string productId, bool consumable, string fallbackPrice)
        {
            ProductId = productId;
            Consumable = consumable;
            FallbackPrice = fallbackPrice;
        }
    }

    /// <summary>A store payment waiting to be fulfilled and confirmed.</summary>
    public sealed class StoreTransaction
    {
        public readonly string ProductId;
        public readonly string TransactionId;
        public readonly string Receipt;

        /// <summary>The gateway's own order object, handed back to it on confirm.</summary>
        public readonly object Handle;

        public StoreTransaction(string productId, string transactionId, string receipt, object handle = null)
        {
            ProductId = productId;
            TransactionId = transactionId;
            Receipt = receipt;
            Handle = handle;
        }
    }

    public enum StoreFailureKind
    {
        /// <summary>The player backed out of the store's dialog.</summary>
        Cancelled,
        /// <summary>The store can't be reached, or doesn't offer this product.</summary>
        Unavailable,
        /// <summary>Anything else: payment declined, duplicate order, unknown error.</summary>
        Failed,
    }

    /// <summary>
    /// The only surface between the game and a billing backend, so nothing outside the gateway touches
    /// a store API. <see cref="UnityIapStoreGateway"/> talks to Unity IAP; <see cref="SimulatedStoreGateway"/>
    /// lets the Shop run in the editor with no store setup.
    /// </summary>
    public interface IStoreGateway
    {
        /// <summary>The product list loaded, so prices and purchases are available. Can fire again after a reconnect.</summary>
        event Action OnReady;

        /// <summary>The store can't be used right now. Carries a log message, not player-facing text.</summary>
        event Action<string> OnUnavailable;

        /// <summary>
        /// A payment went through (possibly in an earlier session). Grant it, persist, then call
        /// <see cref="Confirm"/>; an unconfirmed transaction is redelivered on the next launch.
        /// </summary>
        event Action<StoreTransaction> OnPurchasePending;

        /// <summary>The store reports an already-confirmed one-time purchase, e.g. after a reinstall.</summary>
        event Action<string> OnEntitlementFound;

        /// <summary>(product id, failure kind, log message). Grants nothing.</summary>
        event Action<string, StoreFailureKind, string> OnPurchaseFailed;

        /// <summary>Waiting on approval (Ask to Buy, deferred payment). Grants nothing; <see cref="OnPurchasePending"/> follows if approved.</summary>
        event Action<string> OnPurchaseDeferred;

        bool IsReady { get; }

        void Connect(IReadOnlyList<StoreProductRequest> products);

        /// <summary>The store's localized price, or null when it hasn't provided one.</summary>
        string GetLocalizedPrice(string productId);

        void Purchase(string productId);

        void Confirm(StoreTransaction transaction);

        /// <summary>Asks the store to redeliver one-time purchases. Calls back with success.</summary>
        void Restore(Action<bool> onComplete);

        void Disconnect();
    }

    /// <summary>
    /// Decides whether a store receipt is genuine before anything is granted. Swap in a real validator
    /// (Google Play tangle data, or a server check for Apple's JWS) before shipping.
    /// </summary>
    public interface IReceiptValidator
    {
        bool IsValid(StoreTransaction transaction);
    }

    /// <summary>Accepts every receipt. The default until real validation is configured.</summary>
    public sealed class AcceptAllReceiptValidator : IReceiptValidator
    {
        public bool IsValid(StoreTransaction transaction) => true;
    }

    public static class StoreGatewayFactory
    {
        /// <summary>The simulated store only exists in the editor, so it can never ship in a player.</summary>
        public static IStoreGateway Create()
        {
#if UNITY_EDITOR
            return new SimulatedStoreGateway();
#else
            return new UnityIapStoreGateway();
#endif
        }
    }
}
