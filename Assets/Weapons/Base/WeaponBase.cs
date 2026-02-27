using UnityEngine;

namespace Weapon
{
    public class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected Transform m_FirePoint;
        [SerializeField] protected Bullet m_BulletPrefab;

        protected Crosshair _crosshair;
        
        public float BulletSpeed = 10;
        public float BulletSizeMultiplier = 1;
        public float BulletColliderSizeMultiplier = 1f;     // makes the bullet collider large/small

        public virtual void FireWeapon(Vector2 direction, BulletSource source, Crosshair crosshair = null)
        {
            Bullet bullet = Instantiate(m_BulletPrefab, m_FirePoint.position, Quaternion.identity);
            bullet.SetupScale(BulletSizeMultiplier, BulletColliderSizeMultiplier);
            bullet.Fire(direction, BulletSpeed, source);
            if (source == BulletSource.Player && crosshair != null)
            {
                _crosshair = crosshair;
                _crosshair.SubscribeToBullet(bullet);
            }
        }
    }
}