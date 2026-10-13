using UnityEngine;

namespace Loadout
{
    /// <summary>
    /// Shared fields for every selectable loadout cosmetic. Subclasses add their own visual payload
    /// (head sprite, floatie sprite, weapon stats, …). Having one base lets the LoadoutService treat
    /// all four categories with the same persist / restore / unlock logic.
    /// </summary>
    public abstract class LoadoutItemSO : ScriptableObject
    {
        [Tooltip("Stable id used to persist the player's selection. Never reuse or change once shipped.")]
        public string Id;
        public string DisplayName;

        [Tooltip("Thumbnail shown in the loadout selector strip.")]
        public Sprite Icon;

        [Tooltip("Available from the start. Locked items appear greyed in the selector.")]
        public bool UnlockedByDefault = true;
    }
}
