using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PTL.Framework.Services
{
    /// <summary>
    /// An in-memory store for the editor: prices come from the authored fallbacks and every purchase
    /// succeeds after a short delay, so the whole Shop flow can be played without store setup. Set
    /// <see cref="FailNextPurchaseWith"/> to rehearse a failure.
    /// </summary>
    public sealed class SimulatedStoreGateway : IStoreGateway
    {
        private const int DelayMilliseconds = 350;

        private readonly Dictionary<string, string> _prices = new();
        private bool _disconnected;

        public event Action OnReady;
        public event Action<string> OnUnavailable { add { } remove { } }
        public event Action<StoreTransaction> OnPurchasePending;
        public event Action<string> OnEntitlementFound { add { } remove { } }
        public event Action<string, StoreFailureKind, string> OnPurchaseFailed;
        public event Action<string> OnPurchaseDeferred { add { } remove { } }

        public bool IsReady { get; private set; }

        /// <summary>When set, the next purchase fails this way instead of going through.</summary>
        public StoreFailureKind? FailNextPurchaseWith { get; set; }

        public void Connect(IReadOnlyList<StoreProductRequest> products)
        {
            _prices.Clear();
            foreach (StoreProductRequest product in products)
                _prices[product.ProductId] = product.FallbackPrice;

            IsReady = true;
            OnReady?.Invoke();
        }

        public string GetLocalizedPrice(string productId)
            => _prices.TryGetValue(productId, out string price) ? price : null;

        public async void Purchase(string productId)
        {
            await Task.Delay(DelayMilliseconds);
            if (_disconnected) return;

            if (FailNextPurchaseWith is { } failure)
            {
                FailNextPurchaseWith = null;
                OnPurchaseFailed?.Invoke(productId, failure, "Simulated failure.");
                return;
            }

            OnPurchasePending?.Invoke(new StoreTransaction(productId, $"sim-{Guid.NewGuid():N}", "simulated-receipt"));
        }

        public void Confirm(StoreTransaction transaction) { }

        public void Restore(Action<bool> onComplete) => onComplete?.Invoke(true);

        public void Disconnect()
        {
            _disconnected = true;
            IsReady = false;
        }
    }
}
