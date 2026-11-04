using UnityEngine;

namespace Monetization
{
    /// <summary>
    /// Something the main-menu Shop sells for real money that is not a single loadout item: a coin pack
    /// or an item bundle. Holds only authored data; ownership and balances stay in the services.
    /// </summary>
    public abstract class ShopOfferSO : ScriptableObject
    {
        public string DisplayName;

        [Tooltip("Art shown on the offer's card.")]
        public Sprite Icon;

        [Header("Store")]
        [Tooltip("Store product id. Must match App Store Connect / Play Console. Never reuse or change once shipped.")]
        public string ProductId;

        [Tooltip("Shown until the store reports its localized price, and whenever the store is unavailable.")]
        public string FallbackPrice = "₹49";

        /// <summary>Consumables can be bought repeatedly; everything else is a one-time entitlement.</summary>
        public abstract bool IsConsumable { get; }
    }
}
