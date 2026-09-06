using System.Collections.Generic;
using Enemy.Movement;
using Enemy.Attack;
using Enemy.Death;
using Unity.Cinemachine;
using UnityEngine;
using Utils;

namespace Enemy
{
    public class EnemyController : MonoBehaviour
    {
        /// <summary>
        /// Raised whenever any enemy dies, before it is destroyed. The arena director subscribes
        /// to track alive count / threat budget and to pay down spawn debt.
        /// Static, so subscribers must unsubscribe in OnDestroy.
        /// </summary>
        public static event System.Action<EnemyController> OnAnyEnemyDied;

        /// <summary>Raised when any enemy is spawned and ready. Pairs with <see cref="OnAnyEnemyDied"/>.</summary>
        public static event System.Action<EnemyController> OnAnyEnemySpawned;

        public EnemyType EnemyType;

        [Tooltip("Intrinsic difficulty rating used by the spawn director: how much this enemy counts " +
                 "toward the on-screen threat budget, and how 'hard' it ranks when biasing spawn " +
                 "selection over a section's difficulty ramp. Idle ~1, chaser/shooter higher.")]
        [Min(0f)] public float ThreatCost = 1f;

        public RippleCausingParticleEmitter WaterRippleParticleEmitter;
        public Rigidbody2D RigidBody { get; private set; }
        
        [Header("Effects")]
        [SerializeField] private ShakeIntensity m_DeathShake = ShakeIntensity.Light;

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

        private void Start()
        {
            OnAnyEnemySpawned?.Invoke(this);
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

        public void Die(EnemyDeathParameters parameters)
        {
            // play enemy death SFX and VFX
            WaterRippleParticleEmitter.EmitParticles();     // TODO: Wouldn't do anything as the object would be destroyed this frame. Need to handle this elsewhere.
            _deathHandler?.TriggerDeathEffects(parameters);
            
            CameraShaker.Instance?.Shake(m_DeathShake);

            OnAnyEnemyDied?.Invoke(this);

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