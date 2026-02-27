using UnityEngine;

namespace Weapon
{
    public class WeaponControllerBase : MonoBehaviour
    {
        public WeaponBase ActiveWeapon {get; private set;}

        protected virtual void Start()
        {
            ActiveWeapon = GetComponentInChildren<WeaponBase>();
        }
    }
}