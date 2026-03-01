using System.Collections.Generic;
using Enemy.Movement;
using Enemy.Attack;
using Enemy.Death;
using Unity.Cinemachine;
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
        
        private CinemachineTargetGroup _targetGroup;
        [Space] [SerializeField] private float m_FrameRadius = .5f;

        void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _attack = GetComponent<EnemyAttack>();
            _deathHandler = GetComponent<EnemyDeathEffectHandler>();
            RigidBody = GetComponent<Rigidbody2D>();

            _targetGroup = FindFirstObjectByType<CinemachineTargetGroup>();
            _targetGroup.AddMember(transform, 1, m_FrameRadius);
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
            WaterRippleParticleEmitter.EmitParticles();     // TODO: Wouldn't do anything as the object would be destroyed this frame. Need to handle this elsewhere.
            _deathHandler?.TriggerDeathEffects(parameters);
            _targetGroup.RemoveMember(transform);
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