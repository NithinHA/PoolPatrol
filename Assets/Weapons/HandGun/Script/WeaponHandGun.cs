using System.Collections.Generic;
using UnityEngine;
using Player;
using Enemy;
using Enemy.Death;
using Pooling;

namespace Weapon
{
    public class WeaponHandGun : WeaponBase, IComboWeapon
    {
        [Header("HandGun")]
        [SerializeField] private ParticleSystem m_MuzzleFlash;
        [SerializeField] private ParticleSystem m_ExplosionVfx;
        [Space]
        [Header("Scale")]
        [SerializeField] private float m_RangeLow = 0f;
        [SerializeField] private float m_RangeMid = 1.4f;
        [SerializeField] private float m_RangeHigh = 1.8f;
        [SerializeField] private float m_RangeMax = 2.2f;
        [Header("Color")]
        [SerializeField] private Color m_ComboLowColor = Color.cyan;
        [SerializeField] private Color m_ComboMidColor = Color.yellow;
        [SerializeField] private Color m_ComboHighColor = Color.yellow;
        [SerializeField] private Color m_ComboMaxColor = Color.red;

        public override WeaponAttack FireWeapon(Vector2 direction, BulletSource source)
        {
            // Base class handles ammo consumption, cooldown, and reload.
            // If it returns null the weapon could not fire (reloading / on cooldown).
            WeaponAttack attack = base.FireWeapon(direction, source);
            if (attack != null)
                AnimateOnFire();
            return attack;
        }

        void AnimateOnFire()
        {
            // muzzle flash
            if (m_MuzzleFlash != null)
                m_MuzzleFlash.Play();
            // play knock-back animation
        }

        public void OnComboLevelChanged(PlayerCombo.ComboLevel newLevel)
        {
            // Handgun specific passive changes can go here (e.g., shooting faster)
        }

        public void PerformComboHitEffect(Vector2 hitPosition, PlayerCombo.ComboLevel currentLevel)
        {
            float range = m_RangeLow;
            Color color = m_ComboLowColor;
            switch(currentLevel)
            {
                case PlayerCombo.ComboLevel.Mid:
                    range = m_RangeMid;
                    color = m_ComboMidColor;
                    break;
                case PlayerCombo.ComboLevel.High:
                    range = m_RangeHigh;
                    color = m_ComboHighColor;
                    break;
                case PlayerCombo.ComboLevel.Max:
                    range = m_RangeMax;
                    color = m_ComboMaxColor;
                    break;
                default: return; // Low combo = no explosion
            }

            // Spawn explosion effect at hitPosition with scale = range
            if (m_ExplosionVfx != null)
            {
                PoolableParticles explosion = (PoolableParticles) ObjectPoolManager.Instance.SpawnItem(PoolableItemType.BulletComboExplosion, hitPosition, Quaternion.identity);
                explosion.transform.localScale = Vector3.one * range;
                ParticleSystem.MainModule main = explosion.Particles.main;
                main.startColor = color;
            }

            // Find and damage enemies in radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(hitPosition, range);
            foreach (Collider2D col in colliders)
            {
                if (col.CompareTag(Constants.GameConstants.TAG_Enemy))
                {
                    EnemyController enemy = col.GetComponent<EnemyController>();
                    if (enemy != null)
                    {
                        Vector3 direction = (col.transform.position - (Vector3)hitPosition).normalized;
                        if (direction == Vector3.zero)  // hit the same enemy who was shot.
                            continue;

                        EnemyDeathParameters enemyDeathParams = new EnemyDeathParameters()
                        {
                            CollidingObject = col.gameObject,
                            CollisionDirection = direction
                        };
                        enemy.Die(enemyDeathParams);
                    }
                }
            }
        }
    }
}