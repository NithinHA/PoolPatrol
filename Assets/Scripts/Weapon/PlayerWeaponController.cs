using DG.Tweening;
using Player;
using UnityEngine;

namespace Weapon
{
    public class PlayerWeaponController : WeaponControllerBase
    {
        [SerializeField] private float m_WeaponRotateTweenDuration = .05f;
        [Header("References")]
        [SerializeField] private PlayerController m_PlayerController;
        
#region Unity callbacks

        protected override void Start()
        {
            base.Start();
            m_PlayerController.OnFireInput += OnFireInput;
        }

        private void OnDestroy()
        {
            m_PlayerController.OnFireInput -= OnFireInput;
        }

#endregion

        private void OnFireInput(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180;  // +180 because by default the gun is rotated 180 degree
            transform.DORotate(new Vector3(0,0,angle), m_WeaponRotateTweenDuration, RotateMode.Fast).OnComplete(() =>
            {
                ActiveWeapon.FireWeapon(direction, BulletSource.Player);
            });
        }
    }
}