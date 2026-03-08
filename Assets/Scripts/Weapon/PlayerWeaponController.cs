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
        
#region Unity callbacks

        protected override void Start()
        {
            base.Start();
            m_PlayerController.OnFireInput += OnFireInput;
            UpdateRangeIndicator();
        }

        private void OnDestroy()
        {
            m_PlayerController.OnFireInput -= OnFireInput;
        }

#endregion

        private void OnFireInput(Vector2 direction, Vector2 mousePos)
        {
            Crosshair crosshair = ObjectPoolManager.Instance.SpawnItem(PoolableItemType.Crosshair, mousePos, Quaternion.identity) as Crosshair;
            if(!ActiveWeapon.Magazine.CanFire)
                crosshair?.FailedAttack();

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180;  // +180 because by default the gun is rotated 180 degree
            transform.DORotate(new Vector3(0,0,angle), m_WeaponRotateTweenDuration, RotateMode.Fast).OnComplete(() =>
            {
                Vector2 directionPostRotation = (mousePos - (Vector2)transform.position).normalized;
                WeaponAttack attack = ActiveWeapon.FireWeapon(directionPostRotation, BulletSource);
                if (attack != null)
                {
                    crosshair?.SubscribeToAttack(attack);

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
            });
        }

        public void UpdateRangeIndicator()
        {
            if (m_RangeIndicator != null && ActiveWeapon != null)
            {
                m_RangeIndicator.SetRange(ActiveWeapon.BulletRange);
                m_RangeIndicator.Show();
            }
        }
    }
}