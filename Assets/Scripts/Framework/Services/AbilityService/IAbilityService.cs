using System;
using System.Collections.Generic;
using Abilities;

namespace PTL.Framework.Services
{
    /// <summary>A concrete purchasable offer: one ability at one specific level, with its price.</summary>
    public readonly struct AbilityOffer
    {
        public readonly AbilityDefinition Definition;
        /// <summary>1-based level the player would reach by buying this.</summary>
        public readonly int Level;
        public readonly int Cost;
        public readonly string Description;

        public AbilityOffer(AbilityDefinition definition, int level, int cost, string description)
        {
            Definition = definition;
            Level = level;
            Cost = cost;
            Description = description;
        }

        public bool IsValid => Definition != null && Level >= 1;

        /// <summary>Player-facing title, e.g. "BIGGER BULLETS II" (doc §26).</summary>
        public string DisplayTitle => Definition == null
            ? string.Empty
            : Definition.IsStackable
                ? $"{Definition.DisplayName} {ToRoman(Level)}"
                : Definition.DisplayName;

        private static string ToRoman(int value) => value switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
            _ => value.ToString()
        };
    }

    /// <summary>
    /// An ability the player owns that is still exerting a lasting effect on the run (doc §26 item
    /// 3 — "whether it is permanent for the run"). Purely exhaustible abilities such as "+1 Life",
    /// which spend themselves the instant they are bought, never appear here.
    /// </summary>
    public readonly struct ActiveAbility
    {
        public readonly AbilityDefinition Definition;
        /// <summary>1-based level currently owned.</summary>
        public readonly int Level;

        public ActiveAbility(AbilityDefinition definition, int level)
        {
            Definition = definition;
            Level = level;
        }

        public string DisplayTitle => Definition == null
            ? string.Empty
            : Definition.IsStackable ? $"{Definition.DisplayName} {Level}" : Definition.DisplayName;
    }

    /// <summary>
    /// Owns which abilities the player has acquired this run, decides what the Goddess may offer
    /// (doc §12 eligibility, §17 offer pipeline, §18 weighted selection), and performs purchases
    /// through the economy + run-modifier services.
    /// </summary>
    public interface IAbilityService : IService
    {
        /// <summary>Raised after a successful purchase. Args: (ability, new level).</summary>
        event Action<AbilityDefinition, int> OnAbilityPurchased;

        /// <summary>Raised for each one-shot effect of a purchased level; binders execute these.</summary>
        event Action<InstantEffectType> OnInstantEffect;

        /// <summary>Raised whenever <see cref="ActiveAbilities"/> changes (purchase or run reset).</summary>
        event Action OnActiveAbilitiesChanged;

        /// <summary>Every ability owned this run and its current level, including exhausted one-shots.</summary>
        IReadOnlyDictionary<AbilityDefinition, int> OwnedAbilities { get; }

        /// <summary>
        /// The subset of <see cref="OwnedAbilities"/> whose effects persist for the rest of the
        /// run (bigger bullets, +1 max life, …). This is the list a HUD or debug overlay should
        /// show as "currently active". Cleared by <see cref="ResetRun"/> on level complete or
        /// game over.
        /// </summary>
        IReadOnlyList<ActiveAbility> ActiveAbilities { get; }

        /// <summary>Supplies the ability pool. Called once during setup.</summary>
        void SetDatabase(AbilityDatabase database);

        /// <summary>Supplies live lives/max-lives for <see cref="RunRequirement"/> checks.</summary>
        void SetRunStateProvider(IRunStateProvider provider);

        /// <summary>Sets both filter axes at once. Call once when the arena loads.</summary>
        void SetContext(WeaponKind weapon, ArenaFlags arena);

        /// <summary>Sets only the weapon axis, leaving the arena untouched. Call on weapon switch.</summary>
        void SetWeaponContext(WeaponKind weapon);

        /// <summary>Sets only the arena axis, leaving the weapon untouched. Call when the level loads.</summary>
        void SetArenaContext(ArenaFlags arena);

        /// <summary>Current level of an ability; 0 when not owned.</summary>
        int GetLevel(AbilityDefinition ability);

        /// <summary>True when every level has been purchased.</summary>
        bool IsMaxed(AbilityDefinition ability);

        /// <summary>Runs the full eligibility filter for the current context and run state.</summary>
        bool IsEligible(AbilityDefinition ability);

        /// <summary>
        /// Builds the offer for an ability's next level, if any remains.
        /// </summary>
        bool TryBuildOffer(AbilityDefinition ability, out AbilityOffer offer);

        /// <summary>
        /// Picks <paramref name="count"/> distinct, meaningfully different offers using weighted
        /// selection with category diversity. <paramref name="exclude"/> lets Refresh avoid
        /// re-offering the discarded pair (doc §20).
        /// </summary>
        List<AbilityOffer> GenerateOffers(int count, IEnumerable<AbilityDefinition> exclude = null);

        /// <summary>True when the player can afford the offer and it is still eligible.</summary>
        bool CanPurchase(AbilityOffer offer);

        /// <summary>
        /// Spends the gems and applies the level. Returns false (and changes nothing) if the
        /// player cannot afford it or the offer is stale.
        /// </summary>
        bool TryPurchase(AbilityOffer offer);

        /// <summary>Clears all owned abilities. Called when a new run begins.</summary>
        void ResetRun();
    }
}
