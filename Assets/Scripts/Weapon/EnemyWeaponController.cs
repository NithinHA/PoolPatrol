using UnityEngine;

namespace Weapon
{
    public class EnemyWeaponController : WeaponControllerBase
    {
        public void FireWeapon(Vector2 direction)
        {
            ActiveWeapon.FireWeapon(direction, BulletSource.Enemy);
        }
    }
}