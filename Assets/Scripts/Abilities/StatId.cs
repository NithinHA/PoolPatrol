using System;

namespace Abilities
{
    /// <summary>
    /// Every run-modifiable stat or flag in the game. Abilities declare which of these they change;
    /// <see cref="PTL.Framework.Services.IRunModifierService"/> accumulates them, and the stat
    /// binders push the results into the live player/weapon components.
    ///
    /// Add a new entry here, then teach the relevant binder to read it.
    /// </summary>
    public enum StatId
    {
        // ── General (player) ────────────────────────────────────────────────
        /// <summary>ImpulseMover.m_TargetSpeed — base drift speed.</summary>
        MoveSpeed,
        /// <summary>ImpulseMover.m_ImpulseStrength — recoil propulsion force.</summary>
        Propulsion,
        /// <summary>PlayerHealth.MaxHealth.</summary>
        MaxLives,
        /// <summary>Flat reduction to each combo-level threshold ("Faster Combo").</summary>
        ComboThresholdReduction,
        /// <summary>Bonus pickup radius on collectables ("Gem Magnet").</summary>
        GemMagnetRadius,

        // ── Weapon ──────────────────────────────────────────────────────────
        BulletRange,
        BulletSpeed,
        BulletSize,
        /// <summary>Percent here means "reloads this much faster" (divides reload time).</summary>
        ReloadSpeed,
        MagazineSize,
        /// <summary>Percent here means "fires this much faster" (divides cooldown).</summary>
        FireRate,

        // ── Grenade launcher (future weapon) ────────────────────────────────
        ExplosionRadius,
        HomingDuration,

        // ── Flags (ModifierType.Flag) ───────────────────────────────────────
        BulletRebound,
        PiercingShot,
        ToxicImmunity,
        StormImmunity,
        FoamBreaker,
        VineCutter,
        DamageRevenge,
    }

    /// <summary>How a <see cref="StatModifier"/> combines with the base value.</summary>
    public enum ModifierType
    {
        /// <summary>Added to the base value before percentages apply.</summary>
        Flat,
        /// <summary>Fractional bonus (0.25 = +25%). All percents on a stat sum, then multiply.</summary>
        Percent,
        /// <summary>Boolean capability toggle. <see cref="StatModifier.Value"/> is ignored.</summary>
        Flag,
    }

    /// <summary>A single stat change contributed by one ability level.</summary>
    [Serializable]
    public struct StatModifier
    {
        public StatId Stat;
        public ModifierType Type;
        public float Value;
    }
}
