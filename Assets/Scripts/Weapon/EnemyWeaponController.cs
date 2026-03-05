using UnityEngine;

namespace Weapon
{
    public class EnemyWeaponController : WeaponControllerBase
    {
        public void FireWeapon(Vector2 direction)
        {
            WeaponAttack attack = ActiveWeapon.FireWeapon(direction, BulletSource);
            // Enemy ignores the attack events, as it has no crosshair or combo
        }
    }
}