using System;
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

        /// <summary>The reload style configured for this weapon.</summary>
        public ReloadStyle ReloadStyle { get; private set; }

        // ── Discrete events for UI animation ───────────────────────────────

        /// <summary>A bullet was successfully consumed. Passes the new ammo count.</summary>
        public event Action<int> OnBulletFired;

        /// <summary>Auto-reload has begun (magazine is now empty).</summary>
        public event Action OnReloadStarted;

        /// <summary>
        /// (BoltAction only) One bullet finished loading. Passes the new ammo count.
        /// UI can light up that specific bullet icon.
        /// </summary>
        public event Action<int> OnBulletReloaded;

        /// <summary>The full magazine is ready. Passes the restored ammo count.</summary>
        public event Action<int> OnReloadComplete;

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
        public bool CanFire
        {
            get
            {
                if (CooldownProgress < 1f) return false;
                if (IsReloading)
                {
                    // BoltAction can fire mid-reload if at least one bullet is loaded.
                    if (_reloadStyle == ReloadStyle.BoltAction && CurrentAmmo > 0) return true;
                    return false;
                }
                return CurrentAmmo > 0;
            }
        }

        private ReloadStyle _reloadStyle;
        private float       _reloadTimePerBullet;   // seconds
        private float       _cooldownDuration;      // seconds

        private float _cooldownTimer;   // counts up to _cooldownDuration
        private float _reloadTimer;     // counts up to _reloadTimePerBullet

        public WeaponMagazine(WeaponAmmoSettings settings)
        {
            ApplySettings(settings);

            CurrentAmmo      = MagazineSize;
            CooldownProgress = 1f;   // ready to fire from the start
            ReloadProgress   = 0f;
            IsReloading      = false;
        }

        /// <summary>
        /// Re-applies tuning at runtime (used by run upgrades: +1 magazine, faster reload).
        /// Ammo is preserved and clamped to the new size; any in-progress reload is restarted so
        /// the new timings take effect immediately. Grown magazines do not auto-fill — the extra
        /// round is earned by reloading.
        /// </summary>
        public void Reconfigure(WeaponAmmoSettings settings)
        {
            ApplySettings(settings);

            CurrentAmmo = Mathf.Min(CurrentAmmo, MagazineSize);

            if (IsReloading)
            {
                _reloadTimer   = 0f;
                ReloadProgress = 0f;
            }
            else if (CurrentAmmo < MagazineSize && CurrentAmmo == 0)
            {
                // Still empty under the new settings — make sure a reload is running.
                BeginReload();
            }
        }

        private void ApplySettings(WeaponAmmoSettings settings)
        {
            MagazineSize         = Mathf.Max(1, settings.MagazineSize);
            ReloadStyle          = settings.ReloadStyle;
            _reloadStyle         = settings.ReloadStyle;
            _reloadTimePerBullet = Mathf.Max(settings.ReloadTimePerBullet, 0.0001f); // avoid /0
            _cooldownDuration    = Mathf.Max(0f, settings.CooldownDuration);
        }

        /// <summary>Instantly refills the magazine and cancels any reload (RefillMagazine effect).</summary>
        public void Refill()
        {
            CurrentAmmo    = MagazineSize;
            IsReloading    = false;
            _reloadTimer   = 0f;
            ReloadProgress = 0f;
            OnReloadComplete?.Invoke(CurrentAmmo);
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

        /// <summary>
        /// Attempts to consume one bullet.
        /// Returns true => bullet may be fired; ammo decremented; cooldown reset.
        /// Returns false => cannot fire right now (reloading or on cooldown); a reload interrupt is applied if applicable.
        /// </summary>
        public bool TryConsumeBullet()
        {
            if (CooldownProgress < 1f)
                return false;

            if (IsReloading)
            {
                if (_reloadStyle == ReloadStyle.BoltAction && CurrentAmmo > 0)
                {
                    // Allowed to fire bolt action mid-reload if we have ammo.
                    // This will consume one of the ALREADY loaded bullets. The bullet currently being loaded will have its timer reset.
                    InterruptReload();
                }
                else
                {
                    // MagazineAtOnce OR BoltAction with 0 ammo.
                    InterruptReload();
                    return false;
                }
            }
            else if (CurrentAmmo <= 0)
            {
                // Not reloading but empty? Trigger auto-reload.
                BeginReload();
                return false;
            }

            // Shoot successful
            CurrentAmmo--;
            _cooldownTimer   = 0f;
            CooldownProgress = (_cooldownDuration <= 0f) ? 1f : 0f;

            OnBulletFired?.Invoke(CurrentAmmo);

            // Ensure we are in/enter reloading state if magazine is no longer full (for BoltAction) 
            // or if we just emptied it. 
            // Note: MagazineAtOnce only auto-starts when empty (handled above or below).
            if (IsReloading || CurrentAmmo <= 0)
            {
                if (!IsReloading)
                    BeginReload();
            }

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
                    OnReloadComplete?.Invoke(CurrentAmmo);
                    break;

                case ReloadStyle.BoltAction:
                    CurrentAmmo++;
                    OnBulletReloaded?.Invoke(CurrentAmmo);
                    if (CurrentAmmo >= MagazineSize)
                    {
                        // Magazine fully reloaded.
                        IsReloading    = false;
                        ReloadProgress = 0f;
                        OnReloadComplete?.Invoke(CurrentAmmo);
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
            OnReloadStarted?.Invoke();
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
