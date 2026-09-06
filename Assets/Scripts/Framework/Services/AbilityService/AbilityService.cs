using System;
using System.Collections.Generic;
using Abilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Default <see cref="IAbilityService"/>.
    ///
    /// Eligibility is strict and selection is weighted (doc §18): the Goddess never shows a
    /// nonsense upgrade, but also never guarantees the mathematically best one.
    /// </summary>
    public class AbilityService : IAbilityService
    {
        private static readonly Dictionary<AbilityRarity, float> RarityWeights = new()
        {
            { AbilityRarity.Common,   1.00f },
            { AbilityRarity.Uncommon, 0.60f },
            { AbilityRarity.Rare,     0.30f },
            { AbilityRarity.Epic,     0.12f },
        };

        private readonly Dictionary<AbilityDefinition, int> _owned = new();

        // Reusable scratch buffers so offer generation does not allocate per call.
        private readonly List<AbilityDefinition> _candidates = new();
        private readonly List<AbilityOffer> _offers = new();

        private AbilityDatabase _database;
        private IRunStateProvider _runState;
        private WeaponKind _weapon = WeaponKind.Any;
        private ArenaFlags _arena = ArenaFlags.Any;

        public event Action<AbilityDefinition, int> OnAbilityPurchased;
        public event Action<InstantEffectType> OnInstantEffect;

        public IReadOnlyDictionary<AbilityDefinition, int> OwnedAbilities => _owned;

#region Default callbacks

        public void Start() { }

        public void OnDestroy()
        {
            OnAbilityPurchased = null;
            OnInstantEffect = null;
            _owned.Clear();
        }

#endregion

#region Setup

        public void SetDatabase(AbilityDatabase database) => _database = database;

        public void SetRunStateProvider(IRunStateProvider provider) => _runState = provider;

        public void SetContext(WeaponKind weapon, ArenaFlags arena)
        {
            _weapon = weapon;
            _arena = arena;
        }

#endregion

#region Queries

        public int GetLevel(AbilityDefinition ability)
            => ability != null && _owned.TryGetValue(ability, out int level) ? level : 0;

        public bool IsMaxed(AbilityDefinition ability)
            => ability != null && GetLevel(ability) >= ability.MaxLevel;

        public bool IsEligible(AbilityDefinition ability)
        {
            if (ability == null || ability.MaxLevel == 0)
                return false;

            // Already fully upgraded (doc §15).
            if (IsMaxed(ability))
                return false;

            // Weapon / arena compatibility (doc §14, §11).
            if ((ability.WeaponFilter & _weapon) == 0)
                return false;
            if ((ability.ArenaFilter & _arena) == 0)
                return false;

            // Blocked by a conflicting ability.
            foreach (AbilityDefinition conflict in ability.Conflicts)
            {
                if (conflict != null && GetLevel(conflict) > 0)
                    return false;
            }

            // Prerequisites must all be owned.
            foreach (AbilityDefinition prerequisite in ability.Prerequisites)
            {
                if (prerequisite != null && GetLevel(prerequisite) == 0)
                    return false;
            }

            return MeetsRunRequirement(ability.RunRequirement);
        }

        private bool MeetsRunRequirement(RunRequirement requirement)
        {
            if (requirement == RunRequirement.None)
                return true;

            // Without run state we cannot verify the gate — fail closed so we never offer nonsense.
            if (_runState == null)
                return false;

            return requirement switch
            {
                RunRequirement.LivesBelowMax    => _runState.CurrentLives < _runState.MaxLives,
                RunRequirement.MaxLivesBelowCap => _runState.MaxLives < _runState.MaxLivesCap,
                _ => true,
            };
        }

        public bool TryBuildOffer(AbilityDefinition ability, out AbilityOffer offer)
        {
            offer = default;
            if (ability == null)
                return false;

            int nextLevel = GetLevel(ability) + 1;
            if (!ability.TryGetLevel(nextLevel, out AbilityLevel data))
                return false;

            offer = new AbilityOffer(ability, nextLevel, data.Cost, data.Description);
            return true;
        }

#endregion

#region Offer generation (doc §17)

        public List<AbilityOffer> GenerateOffers(int count, IEnumerable<AbilityDefinition> exclude = null)
        {
            _offers.Clear();

            if (_database == null)
            {
                Debug.LogError("[AbilityService] No AbilityDatabase assigned — cannot generate offers.");
                return new List<AbilityOffer>();
            }

            // 1-4. Build the eligible candidate pool.
            _candidates.Clear();
            foreach (AbilityDefinition ability in _database.Abilities)
            {
                if (ability == null || ability.SelectionWeight <= 0f)
                    continue;
                if (!IsEligible(ability))
                    continue;
                if (exclude != null && Contains(exclude, ability))
                    continue;

                _candidates.Add(ability);
            }

            // 5-7. Weighted pick, preferring a different category for each subsequent offer.
            var pickedCategories = new HashSet<AbilityCategory>();
            while (_offers.Count < count && _candidates.Count > 0)
            {
                AbilityDefinition picked = WeightedPick(_candidates, pickedCategories);
                if (picked == null)
                    break;

                _candidates.Remove(picked);

                if (TryBuildOffer(picked, out AbilityOffer offer))
                {
                    _offers.Add(offer);
                    pickedCategories.Add(picked.Category);
                }
            }

            return new List<AbilityOffer>(_offers);
        }

        /// <summary>
        /// Weighted random pick. Candidates in an already-offered category are down-weighted
        /// rather than excluded, so a small pool still fills every slot (doc §17 step 6).
        /// </summary>
        private static AbilityDefinition WeightedPick(
            List<AbilityDefinition> candidates, HashSet<AbilityCategory> usedCategories)
        {
            const float duplicateCategoryPenalty = 0.25f;

            float total = 0f;
            foreach (AbilityDefinition candidate in candidates)
                total += EffectiveWeight(candidate, usedCategories, duplicateCategoryPenalty);

            if (total <= 0f)
                return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;

            float roll = Random.value * total;
            foreach (AbilityDefinition candidate in candidates)
            {
                roll -= EffectiveWeight(candidate, usedCategories, duplicateCategoryPenalty);
                if (roll <= 0f)
                    return candidate;
            }

            return candidates[candidates.Count - 1];
        }

        private static float EffectiveWeight(
            AbilityDefinition ability, HashSet<AbilityCategory> usedCategories, float penalty)
        {
            float rarity = RarityWeights.TryGetValue(ability.Rarity, out float weight) ? weight : 1f;
            float value = ability.SelectionWeight * rarity;
            if (usedCategories.Contains(ability.Category))
                value *= penalty;
            return value;
        }

        private static bool Contains(IEnumerable<AbilityDefinition> source, AbilityDefinition target)
        {
            foreach (AbilityDefinition item in source)
            {
                if (item == target)
                    return true;
            }
            return false;
        }

#endregion

#region Purchase

        public bool CanPurchase(AbilityOffer offer)
        {
            if (!offer.IsValid)
                return false;

            // Guard against a stale offer: the level must still be the next one up.
            if (GetLevel(offer.Definition) + 1 != offer.Level)
                return false;

            IEconomyService economy = ServiceLocator.GetEconomyService();
            return economy != null && economy.CanAfford(CurrencyType.Gems, offer.Cost);
        }

        public bool TryPurchase(AbilityOffer offer)
        {
            if (!CanPurchase(offer))
                return false;

            if (!offer.Definition.TryGetLevel(offer.Level, out AbilityLevel data))
                return false;

            IEconomyService economy = ServiceLocator.GetEconomyService();
            if (economy == null || !economy.TrySpend(CurrencyType.Gems, offer.Cost))
                return false;

            // Lasting stat changes flow through the run-modifier layer (doc §23).
            ServiceLocator.GetRunModifierService()?.AddRange(data.Modifiers);

            _owned[offer.Definition] = offer.Level;

            // One-shot effects are executed by whoever owns the target (e.g. PlayerStatBinder).
            foreach (InstantEffectType effect in data.InstantEffects)
                OnInstantEffect?.Invoke(effect);

            OnAbilityPurchased?.Invoke(offer.Definition, offer.Level);
            return true;
        }

#endregion

        public void ResetRun()
        {
            _owned.Clear();
        }
    }
}
