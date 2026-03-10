using System.Collections.Generic;
using Player;
using UnityEngine;
using UnityEngine.UI;
using Weapon;

namespace UI.HUD
{
    public class WeaponPanel : MonoBehaviour
    {
        [SerializeField] private BulletIcon      m_BulletIconPrefab;
        [SerializeField] private Transform       m_BulletsLayout;     // Vertical Layout Group parent
        [SerializeField] private Image           m_WeaponIcon;        // Swapped when weapon changes
        [SerializeField] private CooldownIndicator m_CooldownIndicator;

        private WeaponMagazine _magazine;
        private ReloadStyle _reloadStyle;
        private readonly List<BulletIcon> _icons = new List<BulletIcon>();

        private bool _pollReload;   // True only while a reload is in progress — gates the Update poll.

        private void Start()
        {
            PlayerController localPlayer = PlayerController.Local;
            if (localPlayer == null)
            {
                Debug.LogWarning("[WeaponPanel] PlayerController.Local is null. " +
                                 "Ensure PlayerController.Local is set before this Start().");
                return;
            }

            BindWeapon(localPlayer.PlayerWeaponController.ActiveWeapon);

            // TODO: subscribe to weapon-switch event on WeaponController when that feature is added.
        }

        private void OnDestroy()
        {
            UnbindMagazine();
        }

        private void Update()
        {
            if (!_pollReload || _magazine == null)
                return;

            float t = _magazine.ReloadProgress;

            switch (_reloadStyle)
            {
                case ReloadStyle.MagazineAtOnce:
                    // Drive all spent icons simultaneously.
                    for (int i = _magazine.CurrentAmmo; i < _icons.Count; i++)
                        _icons[i].SetReloadProgress(t);
                    break;

                case ReloadStyle.BoltAction:
                    // Drive only the bullet currently being loaded
                    // (the one just above the last fully loaded ammo count).
                    int loadingIndex = _magazine.CurrentAmmo; // 0-based index of bullet in progress
                    if (loadingIndex >= 0 && loadingIndex < _icons.Count)
                        _icons[loadingIndex].SetReloadProgress(t);
                    break;
            }
        }

        /// <summary>
        /// Binds to a new weapon (called on Start and on weapon switch).
        /// Unsubscribes from the old magazine first.
        /// </summary>
        public void BindWeapon(WeaponBase weapon)
        {
            UnbindMagazine();

            if (weapon == null) return;

            _magazine     = weapon.Magazine;
            _reloadStyle  = _magazine.ReloadStyle;

            RebuildIcons(_magazine.MagazineSize, _magazine.CurrentAmmo);

            _magazine.OnBulletFired += OnBulletFired;
            _magazine.OnReloadStarted += OnReloadStarted;
            _magazine.OnBulletReloaded += OnBulletReloaded;
            _magazine.OnReloadComplete += OnReloadComplete;

            m_CooldownIndicator?.Bind(_magazine);
        }

        private void UnbindMagazine()
        {
            if (_magazine == null) return;

            _magazine.OnBulletFired -= OnBulletFired;
            _magazine.OnReloadStarted -= OnReloadStarted;
            _magazine.OnBulletReloaded -= OnBulletReloaded;
            _magazine.OnReloadComplete -= OnReloadComplete;

            m_CooldownIndicator?.Bind(null);
            _magazine = null;
        }

        private void RebuildIcons(int magazineSize, int currentAmmo)
        {
            // Remove excess icons
            while (_icons.Count > magazineSize)
            {
                Destroy(_icons[^1].gameObject);
                _icons.RemoveAt(_icons.Count - 1);
            }

            // Add missing icons
            while (_icons.Count < magazineSize)
            {
                BulletIcon icon = Instantiate(m_BulletIconPrefab, m_BulletsLayout);
                _icons.Add(icon);
            }

            // Set initial state without animation
            for (int i = 0; i < _icons.Count; i++)
            {
                if (i < currentAmmo) 
                {
                    _icons[i].SetLive();
                }
                else 
                {
                    _icons[i].SetSpent();
                    if (_magazine.IsReloading)
                        _icons[i].BeginReload(_reloadStyle);
                }
            }

            _pollReload = _magazine.IsReloading;
        }

        private void OnBulletFired(int newAmmo)
        {
            int spentIndex = newAmmo;
            if (spentIndex >= 0 && spentIndex < _icons.Count)
                _icons[spentIndex].SetSpent();

            if (_pollReload && _reloadStyle == ReloadStyle.BoltAction)
            {
                // Reset visual for bullet that was being loaded (gap shifted)
                int interruptedIndex = newAmmo + 1; 
                if (interruptedIndex < _icons.Count)
                    _icons[interruptedIndex].SetSpent();

                // Start reload visual for newly fired slot
                if (spentIndex >= 0 && spentIndex < _icons.Count)
                    _icons[spentIndex].BeginReload(_reloadStyle);
            }
        }

        private void OnReloadStarted()
        {
            _pollReload = true;

            if (_magazine == null) return;
            for (int i = _magazine.CurrentAmmo; i < _icons.Count; i++)
                _icons[i].BeginReload(_reloadStyle);
        }

        private void OnBulletReloaded(int newAmmo)
        {
            int completedIndex = newAmmo - 1;
            if (completedIndex >= 0 && completedIndex < _icons.Count)
                _icons[completedIndex].CompleteReload();

            // Ensure next slot has fill active (might have been hidden by a previous interrupt)
            if (_magazine != null && _magazine.IsReloading && newAmmo < _icons.Count)
                _icons[newAmmo].BeginReload(_reloadStyle);
        }

        private void OnReloadComplete(int newAmmo)
        {
            _pollReload = false;
            for (int i = 0; i < _icons.Count; i++)
                _icons[i].CompleteReload();
        }
    }
}
