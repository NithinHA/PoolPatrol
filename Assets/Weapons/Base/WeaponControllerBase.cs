using UnityEngine;

namespace Weapon
{
    public class WeaponControllerBase : MonoBehaviour
    {
        public WeaponBase ActiveWeapon {get; private set;}
        public BulletSource BulletSource;

        protected virtual void Start()
        {
            ActiveWeapon = GetComponentInChildren<WeaponBase>();
            ActiveWeapon.Setup(this);   // This must be called whenever the player changes their ActiveWeapon.
        }
    }
}
