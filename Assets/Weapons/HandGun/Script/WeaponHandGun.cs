using UnityEngine;

namespace Weapon
{
    public class WeaponHandGun : WeaponBase
    {
        [Header("HandGun")]
        [SerializeField] private ParticleSystem m_MuzzleFlash;

        public override void FireWeapon(Vector2 direction, BulletSource source, Crosshair crosshair)
        {
            base.FireWeapon(direction, source, crosshair);
            AnimateOnFire();
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