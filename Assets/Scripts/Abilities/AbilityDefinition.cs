using System.Collections.Generic;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// One level of an ability: what it costs, what it says, and what it changes.
    /// Level N is <c>Levels[N - 1]</c> (levels are 1-based to the player; 0 means "not owned").
    /// </summary>
    [System.Serializable]
    public class AbilityLevel
    {
        [Min(0)]
        [Tooltip("Gem cost to purchase this level.")]
        public int Cost = 50;

        [Tooltip("Short player-facing effect text, e.g. \"Bullet size +25%\".")]
        public string Description;

        [Tooltip("Lasting stat changes applied when this level is bought.")]
        public List<StatModifier> Modifiers = new();

        [Tooltip("One-shot effects applied at purchase (e.g. restore a life).")]
        public List<InstantEffectType> InstantEffects = new();
    }

    /// <summary>
    /// Data-driven ability definition (doc §16 "levels, not unrelated upgrades" and §24 "tags").
    /// Create via Assets → Create → PoolPatrol → Ability, then add it to an
    /// <see cref="AbilityDatabase"/> so the Goddess can offer it.
    ///
    /// A one-shot ability simply has a single entry in <see cref="Levels"/>; a stackable one has
    /// several, and becomes ineligible once the last is owned (doc §15).
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Ability", fileName = "Ability_")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id used for logs and save data. Must be unique.")]
        public string Id;
        public string DisplayName;
        public Sprite Icon;
        public AbilityRarity Rarity = AbilityRarity.Common;

        [Header("Eligibility filters")]
        public AbilityCategory Category = AbilityCategory.General;

        [Tooltip("Weapons this ability applies to. Use Any for weapon-agnostic abilities.")]
        public WeaponKind WeaponFilter = WeaponKind.Any;

        [Tooltip("Arenas this ability may appear in. Use Any for universal abilities.")]
        public ArenaFlags ArenaFilter = ArenaFlags.Any;

        [Tooltip("Live run-state gate, e.g. only offer \"+1 Life\" when a life is missing.")]
        public RunRequirement RunRequirement = RunRequirement.None;

        [Header("Levels (index 0 = level 1)")]
        public List<AbilityLevel> Levels = new();

        [Header("Gating")]
        [Tooltip("All of these must be owned (level >= 1) before this can be offered.")]
        public List<AbilityDefinition> Prerequisites = new();

        [Tooltip("If any of these is owned, this ability is never offered.")]
        public List<AbilityDefinition> Conflicts = new();

        [Header("Selection")]
        [Min(0f)]
        [Tooltip("Relative pick weight. 0 = never randomly offered (still purchasable if forced).")]
        public float SelectionWeight = 1f;

        /// <summary>Highest purchasable level. 1 for one-shot abilities.</summary>
        public int MaxLevel => Levels?.Count ?? 0;

        /// <summary>True when the ability has more than one level (doc §15 "stackable").</summary>
        public bool IsStackable => MaxLevel > 1;

        /// <summary>Fetches data for a 1-based level. Returns false if out of range.</summary>
        public bool TryGetLevel(int level, out AbilityLevel data)
        {
            if (Levels != null && level >= 1 && level <= Levels.Count)
            {
                data = Levels[level - 1];
                return true;
            }
            data = null;
            return false;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(Id))
                Id = name;
            if (string.IsNullOrEmpty(DisplayName))
                DisplayName = name;
        }
    }
}
