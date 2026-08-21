using System.Collections.Generic;
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
        
        private Crosshair _activeCrosshair;

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
            if (ActiveWeapon != null)
            {
                ActiveWeapon.Magazine.OnReloadComplete += OnReloadProgress;
                ActiveWeapon.Magazine.OnBulletReloaded += OnReloadProgress;
            }
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
            if (ActiveWeapon != null)
            {
                ActiveWeapon.Magazine.OnReloadComplete -= OnReloadProgress;
                ActiveWeapon.Magazine.OnBulletReloaded -= OnReloadProgress;
            }
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
            _selectedWeaponIndex++;
            ActiveWeapon = m_AvailableWeapons[_selectedWeaponIndex % m_AvailableWeapons.Count];
        }
    }
}