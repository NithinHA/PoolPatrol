using System;
using UnityEngine;

namespace Weapon
{
    public class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected Transform m_FirePoint;
        [SerializeField] protected Bullet m_BulletPrefab;

        private WeaponControllerBase _weaponController;

        public float BulletSpeed = 10;
        public float BulletSizeMultiplier = 1;
        public float BulletColliderSizeMultiplier = 1f;     // makes the bullet collider large/small
        public float BulletRange = 20f;

#region Unity callbacks

        private void OnValidate()
        {
            PlayerWeaponController playerWeaponController = _weaponController as PlayerWeaponController;
            if (playerWeaponController != null)
                playerWeaponController.UpdateRangeIndicator();
        }

#endregion
        
        public void Setup(WeaponControllerBase heldByController)
        {
            _weaponController = heldByController;
        }

        /// <summary>
        /// The control reaches here means the player/enemy does not require a reload. They have enough bullets and wish to Fire.
        /// This function will definitely spawn or fetch a bullet from pool, setup, perform Fire and return the new WeaponAttack instance for tracking.
        /// </summary>
        public virtual WeaponAttack FireWeapon(Vector2 direction, BulletSource source)
        {
            Bullet bullet = Instantiate(m_BulletPrefab, m_FirePoint.position, Quaternion.identity);
            bullet.SetupScale(BulletSizeMultiplier, BulletColliderSizeMultiplier);
            
            WeaponAttack attack = new WeaponAttack();
            bullet.Fire(direction, BulletSpeed, source, attack, BulletRange, _weaponController.transform.position);
            
            return attack;
        }
    }
}