using UnityEngine;

namespace Weapon
{
    public class WeaponHandGun : WeaponBase
    {
        public override void FireWeapon(Vector2 direction, BulletSource source)
        {
            base.FireWeapon(direction, source);
            
        }
    }
}