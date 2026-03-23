using UnityEngine;

namespace Weapon
{
    public class WeaponBase : MonoBehaviour
    {
        [SerializeField] protected Transform m_FirePoint;
        [SerializeField] protected Bullet m_BulletPrefab;

        [Header("Ammo / Reload / Cooldown")]
        [SerializeField] private WeaponAmmoSettings m_AmmoSettings = new WeaponAmmoSettings
        {
            MagazineSize         = 6,
            ReloadStyle          = ReloadStyle.MagazineAtOnce,
            ReloadTimePerBullet  = 2f,
            CooldownDuration     = 0f
        };

        private WeaponControllerBase _weaponController;

        public float BulletSpeed = 10;
        public float BulletSizeMultiplier = 1;
        public float BulletColliderSizeMultiplier = 1f;     // makes the bullet collider large/small
        public float BulletRange = 20f;
        
        [Header("UI")]
        [SerializeField] protected ProjectileIndicatorBase m_ProjectileIndicator;
        public ProjectileIndicatorBase ProjectileIndicator => m_ProjectileIndicator;

        /// <summary>
        /// Provides access to ammo count, reload progress, and cooldown progress.
        /// UI classes should poll this directly — no events, no allocations.
        /// </summary>
        public WeaponMagazine Magazine { get; private set; }

#region Unity callbacks

        protected virtual void Awake()
        {
            Magazine = new WeaponMagazine(m_AmmoSettings);
        }

        protected virtual void Update()
        {
            Magazine.Tick(Time.deltaTime);
        }

        private void OnValidate()
        {
            PlayerWeaponController playerWeaponController = _weaponController as PlayerWeaponController;
            if (playerWeaponController != null)
                playerWeaponController.UpdateRangeIndicator();
        }

#endregion

        public void Setup(WeaponControllerBase heldByController)
        {
            _weaponController = heldByController;
        }

        /// <summary>
        /// Attempts to fire the weapon.
        /// Returns null if the weapon cannot fire (cooldown, reloading, or reload interrupted).
        /// Subclasses should call base.FireWeapon first; if it returns null, they must also return null.
        /// </summary>
        public virtual WeaponAttack FireWeapon(Vector2 direction, BulletSource source)
        {
            if (!Magazine.TryConsumeBullet())
                return null;

            Bullet bullet = Instantiate(m_BulletPrefab, m_FirePoint.position, Quaternion.identity);
            bullet.SetupScale(BulletSizeMultiplier, BulletColliderSizeMultiplier);

            WeaponAttack attack = new WeaponAttack();
            bullet.Fire(direction, BulletSpeed, source, attack, BulletRange, _weaponController.transform.position);

            return attack;
        }
    }
}