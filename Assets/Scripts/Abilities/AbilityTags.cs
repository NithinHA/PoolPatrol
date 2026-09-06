using System;

namespace Abilities
{
    /// <summary>Broad grouping used for offer diversity (see doc §17 "category diversity rules").</summary>
    public enum AbilityCategory
    {
        General,
        Weapon,
        Arena,
        Synergy,
    }

    /// <summary>
    /// Weapon identity. Doubles as a filter: an ability sets <see cref="WeaponKind.Any"/> or ORs the
    /// specific weapons it applies to, while a live weapon reports exactly one bit.
    /// </summary>
    [Flags]
    public enum WeaponKind
    {
        None            = 0,
        Handgun         = 1 << 0,
        Shotgun         = 1 << 1,
        GrenadeLauncher = 1 << 2,

        Any = ~0,
    }

    /// <summary>
    /// Arena identity, matching Docs/arena-selection-and-level-specifications.md. Doubles as a
    /// filter the same way <see cref="WeaponKind"/> does.
    /// </summary>
    [Flags]
    public enum ArenaFlags
    {
        None             = 0,
        ShallowEnd       = 1 << 0,
        RitzRipples      = 1 << 1,
        CabanaCarnage    = 1 << 2,
        BiohazardBasin   = 1 << 3,
        FloraFalls       = 1 << 4,
        StratosphereSpa  = 1 << 5,

        Any = ~0,
    }

    /// <summary>Weights the random pick during offer generation (doc §18 — weighted, not optimal).</summary>
    public enum AbilityRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
    }

    /// <summary>
    /// Live run-state gate evaluated at offer time so the Goddess never shows a useless upgrade
    /// (doc §13). Evaluated against the registered <see cref="IRunStateProvider"/>.
    /// </summary>
    public enum RunRequirement
    {
        /// <summary>Always eligible.</summary>
        None,
        /// <summary>Only when the player is missing at least one life (e.g. "+1 Life").</summary>
        LivesBelowMax,
        /// <summary>Only while max lives is under the cap (e.g. "+1 Max Life").</summary>
        MaxLivesBelowCap,
    }

    /// <summary>
    /// One-shot effects applied at purchase rather than as a lasting stat modifier.
    /// The ability service raises these; <c>PlayerStatBinder</c> executes them.
    /// </summary>
    public enum InstantEffectType
    {
        RestoreOneLife,
        RefillMagazine,
    }

    /// <summary>
    /// Supplies live run state for <see cref="RunRequirement"/> checks. Implemented by
    /// <c>PlayerStatBinder</c> and registered with the ability service.
    /// </summary>
    public interface IRunStateProvider
    {
        int CurrentLives { get; }
        int MaxLives { get; }
        int MaxLivesCap { get; }
    }
}
