using UnityEngine;

namespace Weapon
{
    public class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected Transform m_FirePoint;
        [SerializeField] protected Bullet m_BulletPrefab;

        public float BulletSpeed = 10;
        public float BulletSizeMultiplier = 1;

        public virtual void FireWeapon(Vector2 direction, BulletSource source)
        {
            Bullet bullet =  Instantiate(m_BulletPrefab, m_FirePoint.position, Quaternion.identity);
            SetBulletScale(bullet); 
            bullet.Fire(direction, BulletSpeed, source);
        }

        private void SetBulletScale(Bullet bullet)
        {
            Vector3 scale = bullet.transform.localScale;
            bullet.transform.localScale = scale * BulletSizeMultiplier;
            bullet.SetTrailSize(BulletSizeMultiplier);
        }
    }
}