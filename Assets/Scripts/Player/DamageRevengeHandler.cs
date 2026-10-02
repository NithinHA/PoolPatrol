using Abilities;
using Enemy;
using Pooling;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Executes the "Damage Revenge" ability (doc §9): when the player loses a life, every enemy
    /// within <see cref="m_Radius"/> is blasted outwards, buying the player room to recover.
    ///
    /// Attach to the player root, next to <c>PlayerStatBinder</c>. It is inert until the
    /// <see cref="StatId.DamageRevenge"/> flag is set on the run-modifier service, so it can sit
    /// on the prefab permanently.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class DamageRevengeHandler : MonoBehaviour
    {
        [Header("Pushback")]
        [Tooltip("World-space radius of the blast.")]
        [Min(0f)] [SerializeField] private float m_Radius = 4f;

        [Tooltip("Speed each caught enemy is pushed away at (world units/sec), falling off to " +
                 "zero at the radius edge. Applied through EnemyMovement.PushBack, so it works " +
                 "uniformly across every enemy movement type (idle/kinematic included) — tune " +
                 "this value here to change how hard the knockback reads.")]
        [Min(0f)] [SerializeField] private float m_Force = 18f;

        [Tooltip("Spawn a pooled VFX at the player when the blast fires.")]
        [SerializeField] private bool m_SpawnEffect = true;

        [Tooltip("Which pooled VFX to spawn. Ignored when Spawn Effect is off.")]
        [SerializeField] private PoolableItemType m_Effect = PoolableItemType.BulletComboExplosion;

        private readonly Collider2D[] _hits = new Collider2D[32];

        private PlayerHealth _health;
        private IRunModifierService _modifiers;
        private ContactFilter2D _filter;

        private void Awake()
        {
            _health = GetComponent<PlayerController>().PlayerHealth;

            _filter = ContactFilter2D.noFilter;
            _filter.useTriggers = true;
        }

        private void Start()
        {
            _modifiers = ServiceLocator.GetRunModifierService();

            if (_health != null)
                _health.OnDamaged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnDamaged -= OnDamaged;
        }

        private void OnDamaged()
        {
            if (_modifiers == null || !_modifiers.GetFlag(StatId.DamageRevenge))
                return;

            PushNearbyEnemies();
        }

        private void PushNearbyEnemies()
        {
            Vector2 origin = transform.position;
            int count = Physics2D.OverlapCircle(origin, m_Radius, _filter, _hits);

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _hits[i];
                if (hit == null || !hit.CompareTag(Constants.GameConstants.TAG_Enemy))
                    continue;

                EnemyController enemy = hit.GetComponentInParent<EnemyController>();
                if (enemy == null || enemy.Movement == null)
                    continue;

                Vector2 offset = (Vector2)enemy.transform.position - origin;
                float distance = offset.magnitude;

                // Straight up on top of the player: pick an arbitrary direction rather than NaN.
                Vector2 direction = distance > 0.001f ? offset / distance : Random.insideUnitCircle.normalized;

                // Linear falloff so enemies at the rim are only nudged.
                float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(0.001f, m_Radius));

                // Goes through EnemyMovement.PushBack (not Rigidbody2D.AddForce directly): most
                // movement types drive velocity every tick and would instantly cancel a raw
                // AddForce, and AddForce is ignored outright by Kinematic bodies (idle enemies).
                enemy.Movement.PushBack(m_Force * falloff, direction);
            }

            if (m_SpawnEffect)
                ObjectPoolManager.Instance?.SpawnItem(m_Effect, transform.position, Quaternion.identity);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, m_Radius);
        }
    }
}
