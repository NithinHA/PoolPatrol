using UnityEngine;

namespace Weapon
{
    public class WeaponHandGun : WeaponBase
    {
        [Header("HandGun")]
        [SerializeField] private ParticleSystem m_MuzzleFlash;

        public override Bullet FireWeapon(Vector2 direction)
        {
            // perform reload checks here.
            // if can not fire => return null.  // this check can also be 
            Bullet bullet = base.FireWeapon(direction);
            AnimateOnFire();
            return bullet;
        }

        void AnimateOnFire()
        {
            // muzzle flash
            if (m_MuzzleFlash != null)
                m_MuzzleFlash.Play();
            // play knock-back animation
        }
    }
}