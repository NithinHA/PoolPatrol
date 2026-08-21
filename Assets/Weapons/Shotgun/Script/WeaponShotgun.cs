using System.Collections.Generic;
using UnityEngine;
using Player;
using Enemy;
using Enemy.Death;
using Pooling;
using Utils;

namespace Weapon
{
    public class WeaponShotgun : WeaponBase, IComboWeapon
    {
        [Header("Shotgun")]
        [SerializeField] private ParticleSystem m_MuzzleFlash;
        [Space]
        [Header("Angle Ranges")]
        [SerializeField] private float m_AngleLow = 24f;
        [SerializeField] private float m_AngleMid = 36f;
        [SerializeField] private float m_AngleHigh = 48f;
        [SerializeField] private float m_AngleMax = 60f;
        [Space]
        [Header("Effects")]
        [SerializeField] private ShakeIntensity m_ComboShakeBase = ShakeIntensity.Light;
        [SerializeField] private float m_ComboShakeAmplitudePerHit = 0.5f;

        private float _currentAngleRange;

        protected override void Awake()
        {
            base.Awake();
            _currentAngleRange = m_AngleLow;
        }

        public override WeaponAttack FireWeapon(Vector2 direction, BulletSource source)
        {
            if (!Magazine.TryConsumeBullet())
                return null;

            if (m_MuzzleFlash != null)
                m_MuzzleFlash.Play();

            WeaponAttack attack = new WeaponAttack();

            Vector2 origin = _weaponController != null ? (Vector2)_weaponController.transform.position : (Vector2)FirePoint.position;
            
            // Spawn hit/miss particles as visual feedback for the shot
            // (We could do multiple small ones if needed, but one at origin works, or we can just skip it since instant impact)

            Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, BulletRange);
            int enemiesHitCount = 0;
            bool successInvoked = false;
            Vector2 firstHitPosition = Vector2.zero;

            float halfAngle = _currentAngleRange * 0.5f;

            foreach (Collider2D col in colliders)
            {
                if (col.CompareTag(Constants.GameConstants.TAG_Enemy))
                {
                    Vector2 toEnemy = (Vector2)col.transform.position - origin;
                    float angleToEnemy = Vector2.Angle(direction, toEnemy);

                    if (angleToEnemy <= halfAngle)
                    {
                        EnemyController enemy = col.GetComponent<EnemyController>();
                        if (enemy != null)
                        {
                            enemiesHitCount++;
                            
                            EnemyDeathParameters enemyDeathParams = new EnemyDeathParameters()
                            {
                                CollidingObject = col.gameObject,
                                CollisionDirection = toEnemy.normalized
                            };

                            if (!successInvoked)
                            {
                                firstHitPosition = enemy.transform.position;
                                successInvoked = true;
                            }

                            enemy.Die(enemyDeathParams);
                            
                            // Visuals for hit
                            ObjectPoolManager.Instance.SpawnItem(PoolableItemType.BulletHitParticles, col.transform.position, Quaternion.identity);
                        }
                    }
                }
            }

            if (enemiesHitCount == 0)
            {
                // Visuals for missed (could spawn it at the end of the range in the forward direction)
                ObjectPoolManager.Instance.SpawnItem(PoolableItemType.BulletMissedParticles, origin + direction * BulletRange, Quaternion.identity);
            }

            StartCoroutine(ResolveHitRoutine(attack, successInvoked, firstHitPosition));

            return attack;
        }

        private System.Collections.IEnumerator ResolveHitRoutine(WeaponAttack attack, bool success, Vector2 hitPosition)
        {
            // Yield one frame so the caller can subscribe to the WeaponAttack events before they are invoked.
            yield return null;

            if (success)
            {
                if (attack.OnHitSuccess != null)
                    attack.OnHitSuccess.Invoke(hitPosition);
            }
            else
            {
                if (attack.OnHitFail != null)
                    attack.OnHitFail.Invoke();
            }

            if (attack.OnAttackComplete != null)
                attack.OnAttackComplete.Invoke();

            attack.Clear();
        }

        public void OnComboLevelChanged(PlayerCombo.ComboLevel newLevel)
        {
            switch (newLevel)
            {
                case PlayerCombo.ComboLevel.Low:
                    _currentAngleRange = m_AngleLow;
                    break;
                case PlayerCombo.ComboLevel.Mid:
                    _currentAngleRange = m_AngleMid;
                    break;
                case PlayerCombo.ComboLevel.High:
                    _currentAngleRange = m_AngleHigh;
                    break;
                case PlayerCombo.ComboLevel.Max:
                    _currentAngleRange = m_AngleMax;
                    break;
            }

            if (m_ProjectileIndicator != null && m_ProjectileIndicator is ShotgunIndicator shotgunIndicator)
            {
                shotgunIndicator.AngleRange = _currentAngleRange;
            }
        }

        public void PerformComboHitEffect(Vector2 hitPosition, PlayerCombo.ComboLevel currentLevel)
        {
            // The hit area scaling is done passively via OnComboLevelChanged, but we can do dynamic camera shake here.
            // Since we don't pass the exact enemiesHitCount here directly from PlayerWeaponController, we could just do a standard shake 
            // per combo hit, OR we can let FireWeapon handle the shake itself. Wait, Handgun does it here dynamically.
            // Actually, for Shotgun, PerformComboHitEffect is called once per shot hit by PlayerWeaponController. Adding shake here:
            CameraShaker.Instance?.Shake(m_ComboShakeBase, m_ComboShakeAmplitudePerHit);
        }
    }
}
