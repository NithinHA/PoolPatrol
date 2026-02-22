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

        public virtual void FireWeapon(Vector2 direction, BulletSource source, Crosshair crosshair = null)
        {
            Bullet bullet = Instantiate(m_BulletPrefab, m_FirePoint.position, Quaternion.identity);
            SetBulletScale(bullet); 
            bullet.Fire(direction, BulletSpeed, source);
            if (source == BulletSource.Player && crosshair != null)
            {
                _crosshair = crosshair;
                _crosshair.SubscribeToBullet(bullet);
            }
        }

        private void SetBulletScale(Bullet bullet)
        {
            Vector3 scale = bullet.transform.localScale;
            bullet.transform.localScale = scale * BulletSizeMultiplier;
            bullet.SetTrailSize(BulletSizeMultiplier);
        }
    }
}