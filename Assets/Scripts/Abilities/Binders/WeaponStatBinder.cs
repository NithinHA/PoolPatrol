using System.Collections.Generic;
using Abilities;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;

namespace Weapon
{
    /// <summary>
    /// Pushes run modifiers into the active weapon, and keeps the ability service's weapon
    /// context in sync so only relevant weapon upgrades are offered (doc §14).
    /// Attach to the same GameObject as <see cref="PlayerWeaponController"/>.
    ///
    /// Baselines are captured per weapon the first time it is seen, so switching weapons and
    /// re-applying never compounds values.
    /// </summary>
    [RequireComponent(typeof(PlayerWeaponController))]
    public class WeaponStatBinder : MonoBehaviour
    {
        private readonly struct WeaponBaseline
        {
            public readonly float Range;
            public readonly float Speed;
            public readonly float Size;
            public readonly WeaponAmmoSettings Ammo;

            public WeaponBaseline(WeaponBase weapon)
            {
                Range = weapon.BulletRange;
                Speed = weapon.BulletSpeed;
                Size  = weapon.BulletSizeMultiplier;
                Ammo  = weapon.BaseAmmoSettings;
            }
        }

        private readonly Dictionary<WeaponBase, WeaponBaseline> _baselines = new();

        private PlayerWeaponController _weaponController;
        private IRunModifierService _modifiers;
        private IAbilityService _abilities;
        private WeaponBase _lastApplied;

        private void Awake()
        {
            _weaponController = GetComponent<PlayerWeaponController>();
        }

        private void Start()
        {
            _modifiers = ServiceLocator.GetRunModifierService();
            _abilities = ServiceLocator.GetAbilityService();

            if (_modifiers != null)
                _modifiers.OnModifiersChanged += ApplyModifiers;

            ApplyModifiers();
        }

        private void OnDestroy()
        {
            if (_modifiers != null)
                _modifiers.OnModifiersChanged -= ApplyModifiers;
        }

        private void Update()
        {
            // PlayerWeaponController.SwitchWeapon has no event; detect the swap and re-apply.
            if (_weaponController.ActiveWeapon != _lastApplied)
                ApplyModifiers();
        }

        private void ApplyModifiers()
        {
            WeaponBase weapon = _weaponController.ActiveWeapon;
            if (weapon == null || _modifiers == null)
                return;

            if (!_baselines.TryGetValue(weapon, out WeaponBaseline baseline))
            {
                baseline = new WeaponBaseline(weapon);
                _baselines[weapon] = baseline;
            }

            weapon.BulletRange          = _modifiers.GetFloat(StatId.BulletRange, baseline.Range);
            weapon.BulletSpeed          = _modifiers.GetFloat(StatId.BulletSpeed, baseline.Speed);
            weapon.BulletSizeMultiplier = _modifiers.GetFloat(StatId.BulletSize,  baseline.Size);

            // Reload and fire-rate bonuses reduce their durations, hence GetInverseFloat.
            WeaponAmmoSettings ammo = baseline.Ammo;
            ammo.MagazineSize        = _modifiers.GetInt(StatId.MagazineSize, baseline.Ammo.MagazineSize);
            ammo.ReloadTimePerBullet = _modifiers.GetInverseFloat(StatId.ReloadSpeed, baseline.Ammo.ReloadTimePerBullet);
            ammo.CooldownDuration    = _modifiers.GetInverseFloat(StatId.FireRate, baseline.Ammo.CooldownDuration);
            weapon.ApplyAmmoSettings(ammo);

            // Keep the range indicator in step with the new range.
            _weaponController.UpdateRangeIndicator();

            if (_lastApplied != weapon)
            {
                _lastApplied = weapon;
                UpdateAbilityContext(weapon);
            }
        }

        private void UpdateAbilityContext(WeaponBase weapon)
        {
            if (_abilities == null)
                return;

            // Preserve whatever arena the director set; only the weapon changes here.
            _abilities.SetContext(weapon.Kind, CurrentArena);
        }

        /// <summary>
        /// Arena currently being played. Layer 2's ArenaDirector will own this; until then every
        /// arena-agnostic ability remains eligible.
        /// </summary>
        public static ArenaFlags CurrentArena { get; set; } = ArenaFlags.Any;
    }
}
