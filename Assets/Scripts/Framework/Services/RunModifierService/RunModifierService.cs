using System;
using System.Collections.Generic;
using Abilities;
using UnityEngine;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Default <see cref="IRunModifierService"/>. Keeps three small maps (flat, percent, flags)
    /// keyed by <see cref="StatId"/> so lookups are allocation-free and binders can re-apply
    /// every stat cheaply whenever something changes.
    /// </summary>
    public class RunModifierService : IRunModifierService
    {
        private readonly Dictionary<StatId, float> _flat = new();
        private readonly Dictionary<StatId, float> _percent = new();
        private readonly HashSet<StatId> _flags = new();

        public event Action OnModifiersChanged;

#region Default callbacks

        public void Start() { }

        public void OnDestroy()
        {
            OnModifiersChanged = null;
            ResetRun();
        }

#endregion

        public void Add(StatModifier modifier)
        {
            ApplyInternal(modifier);
            OnModifiersChanged?.Invoke();
        }

        public void AddRange(IEnumerable<StatModifier> modifiers)
        {
            if (modifiers == null)
                return;

            bool any = false;
            foreach (StatModifier modifier in modifiers)
            {
                ApplyInternal(modifier);
                any = true;
            }

            if (any)
                OnModifiersChanged?.Invoke();
        }

        private void ApplyInternal(StatModifier modifier)
        {
            switch (modifier.Type)
            {
                case ModifierType.Flat:
                    _flat.TryGetValue(modifier.Stat, out float flat);
                    _flat[modifier.Stat] = flat + modifier.Value;
                    break;

                case ModifierType.Percent:
                    _percent.TryGetValue(modifier.Stat, out float percent);
                    _percent[modifier.Stat] = percent + modifier.Value;
                    break;

                case ModifierType.Flag:
                    _flags.Add(modifier.Stat);
                    break;
            }
        }

        public float GetFloat(StatId stat, float baseValue)
            => (baseValue + GetFlatSum(stat)) * (1f + GetPercentSum(stat));

        public int GetInt(StatId stat, int baseValue)
            => Mathf.RoundToInt(GetFloat(stat, baseValue));

        public float GetInverseFloat(StatId stat, float baseValue)
        {
            float scaled = baseValue / (1f + GetPercentSum(stat)) - GetFlatSum(stat);
            return Mathf.Max(0f, scaled);
        }

        public bool GetFlag(StatId stat) => _flags.Contains(stat);

        public float GetFlatSum(StatId stat)
            => _flat.TryGetValue(stat, out float value) ? value : 0f;

        public float GetPercentSum(StatId stat)
            => _percent.TryGetValue(stat, out float value) ? value : 0f;

        public void ResetRun()
        {
            if (_flat.Count == 0 && _percent.Count == 0 && _flags.Count == 0)
                return;

            _flat.Clear();
            _percent.Clear();
            _flags.Clear();
            OnModifiersChanged?.Invoke();
        }
    }
}
