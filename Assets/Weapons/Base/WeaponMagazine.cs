using UnityEngine;

namespace Weapon
{
    /// <summary>
    /// Reload style that determines how a weapon refills its magazine.
    /// </summary>
    public enum ReloadStyle
    {
        /// <summary>
        /// After emptying the magazine the full magazine is reloaded at once
        /// after a single ReloadTime duration (e.g. handgun).
        /// While reloading, a FireWeapon call resets the whole reload timer.
        /// </summary>
        MagazineAtOnce,

        /// <summary>
        /// After emptying the magazine, bullets are loaded one-by-one.
        /// Each bullet takes ReloadTimePerBullet seconds.
        /// A FireWeapon call during a bullet's load resets only that bullet's
        /// timer; already-loaded bullets are retained.
        /// </summary>
        BoltAction
    }

    [System.Serializable]
    public struct WeaponAmmoSettings
    {
        [Min(1)]
        [Tooltip("Maximum rounds per magazine.")]
        public int MagazineSize;

        [Tooltip("How the magazine is replenished after emptying.")]
        public ReloadStyle ReloadStyle;

        [Min(0f)]
        [Tooltip("MagazineAtOnce: total reload duration.\n" +
                 "BoltAction: time to load a single bullet.")]
        public float ReloadTimePerBullet;

        [Min(0f)]
        [Tooltip("Minimum seconds between consecutive shots (0 = instant).")]
        public float CooldownDuration;
    }

    public class WeaponMagazine
    {
        public int CurrentAmmo { get; private set; }
        public int MagazineSize { get; private set; }
        public bool IsReloading { get; private set; }

        /// <summary>
        /// Fire cooldown progress.  0 = just fired (locked), 1 = ready.
        /// Ramps smoothly from 0 to 1 every frame.
        /// </summary>
        public float CooldownProgress { get; private set; }

        /// <summary>
        /// Reload progress for the current bullet (BoltAction) or the full magazine (MagazineAtOnce).  0 = just started, 1 = complete.
        /// Zero when not reloading.
        /// </summary>
        public float ReloadProgress { get; private set; }
        public bool CanFire => !IsReloading && CooldownProgress >= 1f;

        private readonly ReloadStyle _reloadStyle;
        private readonly float       _reloadTimePerBullet;   // seconds
        private readonly float       _cooldownDuration;       // seconds

        private float _cooldownTimer;   // counts up to _cooldownDuration
        private float _reloadTimer;     // counts up to _reloadTimePerBullet

        public WeaponMagazine(WeaponAmmoSettings settings)
        {
            MagazineSize         = settings.MagazineSize;
            _reloadStyle         = settings.ReloadStyle;
            _reloadTimePerBullet = Mathf.Max(settings.ReloadTimePerBullet, 0.0001f); // avoid /0
            _cooldownDuration    = settings.CooldownDuration;

            CurrentAmmo      = MagazineSize;
            CooldownProgress = 1f;   // ready to fire from the start
            ReloadProgress   = 0f;
            IsReloading      = false;
        }

        /// <summary>
        /// Must be called once per frame (from WeaponBase.Update).
        /// Advances cooldown and reload timers.
        /// </summary>
        public void Tick(float deltaTime)
        {
            TickCooldown(deltaTime);
            if (IsReloading)
                TickReload(deltaTime);
        }

        // ── Called by WeaponBase.FireWeapon ─────────────────────────────────

        /// <summary>
        /// Attempts to consume one bullet.
        /// Returns true => bullet may be fired; ammo decremented; cooldown reset.
        /// Returns false => cannot fire right now (reloading or on cooldown); a reload interrupt is applied if applicable.
        /// </summary>
        public bool TryConsumeBullet()
        {
            if (IsReloading)
            {
                // Interrupt: reset the in-progress bullet (or full-mag) timer.
                InterruptReload();
                return false;
            }

            if (CooldownProgress < 1f)
                return false;

            // Shoot successful
            CurrentAmmo--;
            _cooldownTimer   = 0f;
            CooldownProgress = (_cooldownDuration <= 0f) ? 1f : 0f;

            if (CurrentAmmo <= 0)
                BeginReload();

            return true;
        }

        private void TickCooldown(float dt)
        {
            if (_cooldownDuration <= 0f || CooldownProgress >= 1f)
                return;

            _cooldownTimer   += dt;
            CooldownProgress  = Mathf.Clamp01(_cooldownTimer / _cooldownDuration);
        }

        private void TickReload(float dt)
        {
            _reloadTimer  += dt;
            ReloadProgress = Mathf.Clamp01(_reloadTimer / _reloadTimePerBullet);

            if (_reloadTimer < _reloadTimePerBullet)
                return;

            // One bullet (or full mag) just finished loading.
            switch (_reloadStyle)
            {
                case ReloadStyle.MagazineAtOnce:
                    CurrentAmmo    = MagazineSize;
                    IsReloading    = false;
                    ReloadProgress = 0f;
                    break;

                case ReloadStyle.BoltAction:
                    CurrentAmmo++;
                    if (CurrentAmmo >= MagazineSize)
                    {
                        // Magazine fully reloaded.
                        IsReloading    = false;
                        ReloadProgress = 0f;
                    }
                    else
                    {
                        // Begin the next bullet.
                        _reloadTimer   = 0f;
                        ReloadProgress = 0f;
                    }
                    break;
            }
        }

        private void BeginReload()
        {
            IsReloading    = true;
            _reloadTimer   = 0f;
            ReloadProgress = 0f;
        }

        private void InterruptReload()
        {
            // For both styles we reset the in-progress timer only.
            // BoltAction: already-loaded bullets (CurrentAmmo) are preserved; only the bullet currently being loaded is reset.
            // MagazineAtOnce: the whole reload timer resets.
            _reloadTimer   = 0f;
            ReloadProgress = 0f;
        }
    }
}
