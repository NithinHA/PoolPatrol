using System;
using System.Collections.Generic;
using Abilities;
using DG.Tweening;
using Player;
using Pooling;
using UnityEngine;

namespace Weapon
{
    public class PlayerWeaponController : WeaponControllerBase
    {
        [SerializeField] private float m_WeaponRotateTweenDuration = .05f;
        [Header("References")]
        [SerializeField] private PlayerController m_PlayerController;
        [SerializeField] private WeaponRangeIndicator m_RangeIndicator;

        [SerializeField] private List<WeaponBase> m_AvailableWeapons;
        private int _selectedWeaponIndex = 0;

        // The weapon whose magazine events we are currently subscribed to, so re-binding on a weapon
        // switch never double-subscribes or leaks.
        private WeaponBase _boundWeapon;

        private Crosshair _activeCrosshair;

        /// <summary>
        /// Fired whenever the active weapon changes (switch, or the loadout equipping one at game
        /// start). UI that caches per-weapon state (HUD ammo icons, weapon portrait) should rebind here
        /// instead of only reading <see cref="ActiveWeapon"/> once in Start — Start order between the
        /// loadout applier and that UI isn't guaranteed.
        /// </summary>
        public event Action<WeaponBase> OnWeaponChanged;

#region Unity callbacks

        protected override void Awake()
        {
            base.Awake();
            m_PlayerController.OnPointerDownEvent += OnPointerDown;
            m_PlayerController.OnPointerUpdateEvent += OnPointerUpdate;
            m_PlayerController.OnHoldStartEvent += OnHoldStart;
            m_PlayerController.OnHoldUpdateEvent += OnHoldUpdate;
            m_PlayerController.OnFireReleaseEvent += OnFireRelease;
            m_PlayerController.PlayerCombo.OnComboLevelChanged += OnComboLevelChanged;
            UpdateRangeIndicator();
        }

        private void Start()
        {
            // Sync the selected index to whatever weapon base.Awake made active, then bind its events.
            _selectedWeaponIndex = Mathf.Max(0, IndexOf(ActiveWeapon));
            BindActiveWeaponEvents();
        }

        private void OnDestroy()
        {
            if (m_PlayerController != null)
            {
                m_PlayerController.OnPointerDownEvent -= OnPointerDown;
                m_PlayerController.OnPointerUpdateEvent -= OnPointerUpdate;
                m_PlayerController.OnHoldStartEvent -= OnHoldStart;
                m_PlayerController.OnHoldUpdateEvent -= OnHoldUpdate;
                m_PlayerController.OnFireReleaseEvent -= OnFireRelease;
                m_PlayerController.PlayerCombo.OnComboLevelChanged -= OnComboLevelChanged;
            }
            UnbindActiveWeaponEvents();
        }

        private void OnReloadProgress(int currentAmmo)
        {
            if (ActiveWeapon != null && ActiveWeapon.ProjectileIndicator != null && ActiveWeapon.ProjectileIndicator.gameObject.activeSelf)
            {
                ActiveWeapon.ProjectileIndicator.UpdateColor(ActiveWeapon.Magazine.CanFire);
            }
        }

        private void OnComboLevelChanged(PlayerCombo.ComboLevel newLevel)
        {
            if (ActiveWeapon is IComboWeapon comboWeapon)
            {
                comboWeapon.OnComboLevelChanged(newLevel);
            }
        }

#endregion

        private void OnPointerDown(Vector2 mousePos)
        {
            _activeCrosshair = ObjectPoolManager.Instance.SpawnItem(PoolableItemType.Crosshair, mousePos, Quaternion.identity) as Crosshair;
        }

        private void OnPointerUpdate(Vector2 mousePos)
        {
            if (_activeCrosshair != null)
            {
                _activeCrosshair.transform.position = mousePos;
            }

            Vector2 direction = (mousePos - (Vector2)transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void OnHoldStart(Vector2 direction, Vector2 mousePos)
        {
            if (m_RangeIndicator != null) m_RangeIndicator.Hide();
            if (ActiveWeapon != null && ActiveWeapon.ProjectileIndicator != null)
            {
                ActiveWeapon.ProjectileIndicator.Show(ActiveWeapon.Magazine.CanFire);
                ActiveWeapon.ProjectileIndicator.UpdateAim(ActiveWeapon.FirePoint.position, direction, ActiveWeapon.BulletRange);
            }
        }

        private void OnHoldUpdate(Vector2 direction, Vector2 mousePos)
        {
            if (ActiveWeapon != null && ActiveWeapon.ProjectileIndicator != null)
            {
                ActiveWeapon.ProjectileIndicator.UpdateAim(ActiveWeapon.FirePoint.position, direction, ActiveWeapon.BulletRange);
            }
        }

        private void OnFireRelease(Vector2 direction, Vector2 mousePos)
        {
            if (m_RangeIndicator != null) m_RangeIndicator.Show();
            if (ActiveWeapon != null && ActiveWeapon.ProjectileIndicator != null)
            {
                ActiveWeapon.ProjectileIndicator.Hide();
            }

            Crosshair localCrosshair = _activeCrosshair;
            _activeCrosshair = null;

            Vector2 directionPostRotation = (mousePos - (Vector2)transform.position).normalized;
            WeaponAttack attack = ActiveWeapon.FireWeapon(directionPostRotation, BulletSource);
            if (attack != null)
            {
                localCrosshair?.SubscribeToAttack(attack);

                attack.OnHitSuccess += (hitPos) =>
                {
                    m_PlayerController.PlayerCombo.AddCombo(hitPos);
                    if (ActiveWeapon is IComboWeapon comboWeapon)
                    {
                        comboWeapon.PerformComboHitEffect(hitPos, m_PlayerController.PlayerCombo.CurrentComboLevel);
                    }
                };

                attack.OnHitFail += () => { m_PlayerController.PlayerCombo.BreakCombo(); };
            }
            else
            {
                localCrosshair?.FailedAttack();
            }
        }

        public void UpdateRangeIndicator()
        {
            if (m_RangeIndicator != null && ActiveWeapon != null)
            {
                m_RangeIndicator.SetRange(ActiveWeapon.BulletRange);
                m_RangeIndicator.Show();
            }
        }

        public void SwitchWeapon()
        {
            if (m_AvailableWeapons == null || m_AvailableWeapons.Count == 0) return;
            SetActiveWeapon(_selectedWeaponIndex + 1);
        }

        /// <summary>
        /// Equips the available weapon whose <see cref="WeaponBase.Kind"/> matches, driven by the
        /// player's loadout at game start. No-op (with a warning) if that weapon isn't available.
        /// </summary>
        public void SelectWeaponByKind(WeaponKind kind)
        {
            if (m_AvailableWeapons == null || m_AvailableWeapons.Count == 0) return;

            for (int i = 0; i < m_AvailableWeapons.Count; i++)
            {
                if (m_AvailableWeapons[i] != null && m_AvailableWeapons[i].Kind == kind)
                {
                    SetActiveWeapon(i);
                    return;
                }
            }

            Debug.LogWarning($"[PlayerWeaponController] No available weapon of kind {kind}; keeping current.");
        }

        /// <summary>
        /// Makes the weapon at <paramref name="index"/> the active one: shows only it, re-runs Setup,
        /// and re-binds magazine events. Index wraps so it is safe for cycling.
        /// </summary>
        private void SetActiveWeapon(int index)
        {
            int count = m_AvailableWeapons.Count;
            index = ((index % count) + count) % count;
            _selectedWeaponIndex = index;

            for (int i = 0; i < count; i++)
            {
                if (m_AvailableWeapons[i] != null)
                    m_AvailableWeapons[i].gameObject.SetActive(i == index);
            }

            ActiveWeapon = m_AvailableWeapons[index];
            ActiveWeapon.Setup(this);
            BindActiveWeaponEvents();
            UpdateRangeIndicator();
            OnWeaponChanged?.Invoke(ActiveWeapon);
        }

        private int IndexOf(WeaponBase weapon)
        {
            if (m_AvailableWeapons == null || weapon == null) return -1;
            return m_AvailableWeapons.IndexOf(weapon);
        }

        private void BindActiveWeaponEvents()
        {
            UnbindActiveWeaponEvents();
            if (ActiveWeapon == null || ActiveWeapon.Magazine == null) return;

            ActiveWeapon.Magazine.OnReloadComplete += OnReloadProgress;
            ActiveWeapon.Magazine.OnBulletReloaded += OnReloadProgress;
            _boundWeapon = ActiveWeapon;
        }

        private void UnbindActiveWeaponEvents()
        {
            if (_boundWeapon == null || _boundWeapon.Magazine == null)
            {
                _boundWeapon = null;
                return;
            }

            _boundWeapon.Magazine.OnReloadComplete -= OnReloadProgress;
            _boundWeapon.Magazine.OnBulletReloaded -= OnReloadProgress;
            _boundWeapon = null;
        }
    }
}