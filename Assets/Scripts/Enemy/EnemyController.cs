using System.Collections.Generic;
using Enemy.Movement;
using Enemy.Attack;
using Enemy.Death;
using UnityEngine;
using Weapon;

namespace Enemy
{
    public class EnemyController : MonoBehaviour
    {
        public EnemyType EnemyType;
        public ParticleEmitter WaterRippleParticleEmitter;
        public Rigidbody2D RigidBody { get; private set; }
        
        private EnemyMovement _movement;
        private EnemyAttack _attack;
        private EnemyDeathEffectHandler _deathHandler;

        void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _attack = GetComponent<EnemyAttack>();
            _deathHandler = GetComponent<EnemyDeathEffectHandler>();
            RigidBody = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            _movement?.Tick();
            _attack?.Tick();
        }

        private void FixedUpdate()
        {
            _movement?.FixedTick();
        }

        public void Die(Dictionary<string, object> parameters)
        {
            // play enemy death SFX and VFX
            WaterRippleParticleEmitter.EmitParticles();
            _deathHandler?.TriggerDeathEffects(parameters);
            Destroy(gameObject);
        }

        // void OnCollisionEnter2D(Collision2D col)
        // {
        //     if (col.gameObject.CompareTag(Constants.GameConstants.TAG_Bullet))
        //     {
        //         Bullet bullet = col.gameObject.GetComponent<Bullet>();
        //         if (bullet.BulletSource == BulletSource.Player || bullet.BulletSource == BulletSource.Environment)
        //             Die(col);
        //         bullet.DestroyBullet();
        //     }
        // }
    }
}