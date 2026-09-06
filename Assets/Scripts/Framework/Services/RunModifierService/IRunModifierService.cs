using System;
using Abilities;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Accumulates every stat change acquired during the current run (doc §23 — the single
    /// RunModifier layer between abilities and gameplay). Nothing writes player/weapon values
    /// directly from UI; abilities feed this, and the stat binders read from it.
    ///
    /// All modifiers are run-scoped and cleared by <see cref="ResetRun"/>.
    /// </summary>
    public interface IRunModifierService : IService
    {
        /// <summary>Raised after any modifier change so binders can re-apply.</summary>
        event Action OnModifiersChanged;

        /// <summary>Records a modifier. Use <see cref="AddRange"/> when applying a whole ability level.</summary>
        void Add(StatModifier modifier);

        /// <summary>Records several modifiers and raises <see cref="OnModifiersChanged"/> once.</summary>
        void AddRange(System.Collections.Generic.IEnumerable<StatModifier> modifiers);

        /// <summary>
        /// Resolves a stat: <c>(baseValue + flatSum) * (1 + percentSum)</c>.
        /// </summary>
        float GetFloat(StatId stat, float baseValue);

        /// <summary>As <see cref="GetFloat"/>, rounded to the nearest int. For magazine size, lives, …</summary>
        int GetInt(StatId stat, int baseValue);

        /// <summary>
        /// Resolves a "higher is faster" stat by dividing out the percent bonus:
        /// <c>baseValue / (1 + percentSum) - flatSum</c>. Use for reload time and fire cooldown,
        /// where a +25% bonus should mean 25% less time. Never returns below zero.
        /// </summary>
        float GetInverseFloat(StatId stat, float baseValue);

        /// <summary>True if any <see cref="ModifierType.Flag"/> modifier was recorded for this stat.</summary>
        bool GetFlag(StatId stat);

        /// <summary>Raw sum of flat modifiers — for stats with no meaningful base (e.g. bonus radius).</summary>
        float GetFlatSum(StatId stat);

        /// <summary>Raw sum of percent modifiers (0.25 = +25%).</summary>
        float GetPercentSum(StatId stat);

        /// <summary>Clears every modifier. Called when a new run begins.</summary>
        void ResetRun();
    }
}
