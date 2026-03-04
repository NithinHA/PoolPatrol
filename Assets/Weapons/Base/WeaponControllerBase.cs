using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weapon
{
    public class WeaponControllerBase : MonoBehaviour
    {
        public WeaponBase ActiveWeapon {get; private set;}
        public BulletSource BulletSource;

        public Action<Bullet> OnBulletCreated;
        public Action<Vector2> OnHitSuccess;
        public Action OnHitFail;
        public Action<Bullet> OnBulletDestroyed;

        protected virtual void Start()
        {
            ActiveWeapon = GetComponentInChildren<WeaponBase>();
            ActiveWeapon.Setup(this);   // This must be called whenever the player changes their ActiveWeapon.
        }

        /// <summary>
        /// Since WeaponControllers call ActiveWeapon.Fire(), this function gets called by self.
        /// </summary>
        /// <param name="bullet"></param>
        protected virtual void OnBulletCreatedFunction(Bullet bullet)
        {
            OnBulletCreated?.Invoke(bullet);
        }
    }
}