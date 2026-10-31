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

        /// <summary>Real-money purchase (<see cref="LoadoutItemSO.IapProductId"/>). Never allowed for weapons.</summary>
        IAP,
    }

    /// <summary>
    /// Shared fields for every selectable loadout cosmetic. Subclasses add their own visual payload
    /// (head sprite, floatie sprite, weapon stats, …). Having one base lets the LoadoutService treat
    /// all four categories with the same persist / restore / unlock logic.
    /// </summary>
    public abstract class LoadoutItemSO : ScriptableObject
    {
        [Tooltip("Stable id used to persist the player's selection and ownership. Never reuse or change once shipped.")]
        public string Id;
        public string DisplayName;

        [Tooltip("Thumbnail shown in the loadout selector strip.")]
        public Sprite Icon;

        [Header("Unlock")]
        [Tooltip("How the player gets this item. Locked items appear greyed in the selector with a price / reason tag.")]
        public UnlockType Unlock = UnlockType.Free;

        [Tooltip("Coins: one-time price. Starting tiers ~300 / 800 / 1500+.")]
        [Min(0)] public int CoinPrice = 300;

        [Tooltip("ArenaClear: 0-based index of the arena whose completion unlocks this item.")]
        [Min(0)] public int ArenaIndex;

        [Tooltip("IAP: store product id that grants this item.")]
        public string IapProductId;

        /// <summary>Whether this category may be sold for real money. Weapons say no (no pay-to-win).</summary>
        protected virtual bool AllowsIap => true;

        protected virtual void OnValidate()
        {
            if (Unlock == UnlockType.IAP && !AllowsIap)
            {
                Debug.LogWarning($"[{GetType().Name}] '{name}' cannot be an IAP unlock — weapons are coin or " +
                                 "arena-clear only. Switched to Coins.", this);
                Unlock = UnlockType.Coins;
            }
        }
    }
}
