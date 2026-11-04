using UnityEngine;

namespace Loadout
{
    /// <summary>How a loadout item becomes available (Docs/economy-and-progression.md → Unlockables).</summary>
    public enum UnlockType
    {
        /// <summary>Available from the start.</summary>
        Free,

        /// <summary>Bought once with Coins (<see cref="LoadoutItemSO.CoinPrice"/>).</summary>
        Coins,

        /// <summary>Unlocked by completing every level of an arena (<see cref="LoadoutItemSO.ArenaIndex"/>).</summary>
        ArenaClear,

        /// <summary>Real-money purchase (<see cref="LoadoutItemSO.IapProductId"/>).</summary>
        IAP,

        /// <summary>Only obtainable through an item bundle. Not listed on its own in the Shop.</summary>
        BundleOnly,
    }

    /// <summary>
    /// Shared fields for every selectable loadout cosmetic. Subclasses add their own visual payload
    /// (head sprite, floatie sprite, weapon stats, …). Having one base lets the LoadoutService treat
    /// all categories with the same persist / restore / unlock logic, and lets the Shop list and sell
    /// them without a second set of item data.
    /// </summary>
    public abstract class LoadoutItemSO : ScriptableObject
    {
        [Tooltip("Stable id used to persist the player's selection and ownership. Never reuse or change once shipped.")]
        public string Id;
        public string DisplayName;

        [Tooltip("Thumbnail shown in the loadout selector strip.")]
        public Sprite Icon;

        [Header("Unlock")]
        [Tooltip("How the player gets this item. Locked items appear only in the Shop; the loadout screen lists unlocked items.")]
        public UnlockType Unlock = UnlockType.Free;

        [Tooltip("Coins: one-time price. Starting tiers ~300 / 800 / 1500+.")]
        [Min(0)] public int CoinPrice = 300;

        [Tooltip("ArenaClear: 0-based index of the arena whose completion unlocks this item.")]
        [Min(0)] public int ArenaIndex;

        [Tooltip("IAP: store product id that grants this item.")]
        public string IapProductId;

        [Tooltip("IAP: shown until the store reports its localized price, and whenever the store is unavailable.")]
        public string IapFallbackPrice = "₹30";
    }
}
