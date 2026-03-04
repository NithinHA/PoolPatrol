using UnityEngine;

namespace Weapon
{
    public class EnemyWeaponController : WeaponControllerBase
    {
        public void FireWeapon(Vector2 direction)
        {
            Bullet bullet = ActiveWeapon.FireWeapon(direction);
            OnBulletCreatedFunction(bullet);
        }
    }
}