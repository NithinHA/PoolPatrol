using Enemy;
using Enemy.Death;
using Player;
using Pooling;
using UnityEngine;

namespace Weapon
{
    /// <summary>
    /// Run-upgrade behaviours a single bullet carries. Built once per shot by
    /// <see cref="WeaponBase.BuildBulletTraits"/> from the run-modifier flags, so the bullet itself
    /// never has to know the ability system exists.
    /// </summary>
    public readonly struct BulletTraits
    {
        /// <summary>How many extra enemies the bullet passes through ("Piercing Shot").</summary>
        public readonly int Pierces;

        /// <summary>How many times the bullet bounces off arena walls ("Ricochet").</summary>
        public readonly int Bounces;

        public BulletTraits(int pierces, int bounces)
        {
            Pierces = pierces;
            Bounces = bounces;
        }

        public static readonly BulletTraits Default = new(0, 0);
    }

    public class Bullet : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D m_Rb2D;
        [SerializeField] private TrailRenderer m_TrailRenderer;
        [SerializeField] private CircleCollider2D m_Collider;

        private Renderer[] _cachedRenderers;
        private WeaponAttack _attack;
        private BulletSource _source;

        private WaitForSeconds _outOfBoundDelay = new WaitForSeconds(1f);
        private Vector3 _lastFrameVelocity;
        private Vector3 _startPosition;
        private float _sqrRange;

        private bool _isBulletHitSuccess = false;
        private bool _isDestroying = false;

        private int _piercesLeft;
        private int _bouncesLeft;

#region Unity callbacks

        private void Awake()
        {
            _cachedRenderers = GetComponentsInChildren<Renderer>();
        }

        private void FixedUpdate()
        {
            _lastFrameVelocity = m_Rb2D.linearVelocity;

            if (IsExceedingRange())
            {
                InvokeHitFail(false);
            }
        }

        private bool IsExceedingRange()
        {
            return (transform.position - _startPosition).sqrMagnitude > _sqrRange;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag(Constants.GameConstants.TAG_ScreenEdges))
            {
                // "Ricochet": bounce off the wall instead of dying, and treat the bounce point as a
                // fresh origin so the bullet gets its full range again on the new leg.
                if (_bouncesLeft > 0 && TryBounceOff(other))
                {
                    _bouncesLeft--;
                    return;
                }

                InvokeHitFail(true);
            }
            else if (other.gameObject.CompareTag(Constants.GameConstants.TAG_Player))
            {
                PlayerController player = other.gameObject.GetComponent<PlayerController>();
                if (_source == BulletSource.Enemy)
                {
                    _attack?.OnHitSuccess?.Invoke(player.transform.position);
                    player.PlayerHealth.TakeDamage();
                    _isBulletHitSuccess = true;
                }

                if (_source != BulletSource.Player)     // This will prevent friendly fire. Or scenarios where player's bullets hit self collider.
                    DestroyBullet();
            }
            else if (other.gameObject.CompareTag(Constants.GameConstants.TAG_Enemy))
            {
                if (_source == BulletSource.Player || _source == BulletSource.Enemy)    // source==Enemy enables Enemy friendly fire.
                {
                    EnemyController enemy = other.gameObject.GetComponent<EnemyController>();
                    
                    EnemyDeathParameters enemyDeathParams = new EnemyDeathParameters()
                    {
                        CollidingObject = other.gameObject,
                        CollisionDirection = _lastFrameVelocity.normalized
                    };
                    
                    _attack?.OnHitSuccess?.Invoke(enemy.transform.position);
                    enemy.Die(enemyDeathParams);
                    _isBulletHitSuccess = true;

                    // "Piercing Shot": carry straight on through and keep killing.
                    if (_piercesLeft > 0)
                    {
                        _piercesLeft--;
                        return;
                    }
                }

                DestroyBullet();
            }
            // else
            // {
            //     DestroyBullet();    // destroys on collision with anything else!
            // }
        }

#endregion

        public void SetupScale(float sizeMultiplier, float colliderSizeMultiplier)
        {
            transform.localScale *= sizeMultiplier;
            SetTrailSize(sizeMultiplier);
            m_Collider.radius *= colliderSizeMultiplier;
        }

        /// <summary>
        /// Applies the shooter's run upgrades. Call before <see cref="Fire"/>; bullets left
        /// unconfigured simply behave as they always have.
        /// </summary>
        public void Configure(BulletTraits traits)
        {
            _piercesLeft = Mathf.Max(0, traits.Pierces);
            _bouncesLeft = Mathf.Max(0, traits.Bounces);
        }

        public void Fire(Vector2 direction, float speed, BulletSource source, WeaponAttack attack, float range, Vector3 startPosition)
        {
            _source = source;
            _attack = attack;
            _startPosition = startPosition;
            _sqrRange = range * range;
            direction.Normalize();
            m_Rb2D.AddForce(direction * speed, ForceMode2D.Impulse);
        }

        /// <summary>
        /// Reflects the bullet off an arena wall. The wall's inward normal is taken from whichever
        /// face of its box the bullet is closest to, which is exact for the four axis-aligned edge
        /// colliders and degrades gracefully for anything else. Returns false when the bullet has
        /// no usable velocity to reflect.
        /// </summary>
        private bool TryBounceOff(Collider2D wall)
        {
            Vector2 velocity = m_Rb2D.linearVelocity;
            if (velocity.sqrMagnitude < 0.0001f)
                velocity = _lastFrameVelocity;
            if (velocity.sqrMagnitude < 0.0001f)
                return false;

            Vector2 normal = InwardNormal(wall, transform.position);
            Vector2 reflected = Vector2.Reflect(velocity, normal);

            m_Rb2D.linearVelocity = reflected;
            // Nudge clear of the wall so the very next physics step does not re-trigger this collider.
            m_Rb2D.position += normal * 0.05f;

            // The remaining-range check measures from the origin, so re-origin at the bounce.
            _startPosition = transform.position;
            return true;
        }

        /// <summary>Unit normal of the box face nearest <paramref name="point"/>, pointing away from the wall.</summary>
        private static Vector2 InwardNormal(Collider2D wall, Vector2 point)
        {
            Bounds bounds = wall.bounds;
            Vector2 offset = point - (Vector2)bounds.center;
            Vector2 extents = bounds.extents;

            // Compare penetration depth per axis rather than raw distance, so a long thin wall
            // still reports the face the bullet actually came through.
            float xRatio = extents.x > 0.0001f ? Mathf.Abs(offset.x) / extents.x : 0f;
            float yRatio = extents.y > 0.0001f ? Mathf.Abs(offset.y) / extents.y : 0f;

            return xRatio >= yRatio
                ? new Vector2(Mathf.Sign(offset.x), 0f)
                : new Vector2(0f, Mathf.Sign(offset.y));
        }

        private void SetTrailSize(float multiplier)
        {
            float currentSize = m_TrailRenderer.startWidth;
            m_TrailRenderer.startWidth = m_TrailRenderer.endWidth = currentSize * multiplier;
        }

        private void SetTrailColor(Color color)
        {
            m_TrailRenderer.startColor = color;
        }

        private void InvokeHitFail(bool hasBulletCollidedSomething)
        {
            if (!_isBulletHitSuccess)   // just a safety check to avoid cases like hit edge and enemy at the same frame; or hit enemy and out of range in the same frame.
                _attack?.OnHitFail?.Invoke();
            DestroyBullet(hasBulletCollidedSomething);
        }

        private void DestroyBullet(bool hasBulletCollidedSomething = true)
        {
            if (_isDestroying) return;
            _isDestroying = true;

            ObjectPoolManager.Instance.SpawnItem(hasBulletCollidedSomething
                    ? PoolableItemType.BulletHitParticles
                    : PoolableItemType.BulletMissedParticles, transform.position, Quaternion.identity);

            _attack?.OnAttackComplete?.Invoke();
            _attack?.Clear();
            _attack = null;

            // Soft destruction: Disable physics and visuals, let trail fade out
            m_Rb2D.linearVelocity = Vector2.zero;
            m_Rb2D.simulated = false; // Prevents further physics interactions
            m_Collider.enabled = false;

            if (m_TrailRenderer != null)
                m_TrailRenderer.emitting = false;

            // Hide all renderers except the trail itself
            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                Renderer r = _cachedRenderers[i];
                if (r != m_TrailRenderer)
                    r.enabled = false;
            }

            // Destroy the bullet object after the trail duration has passed
            float destroyDelay = m_TrailRenderer != null ? m_TrailRenderer.time : 0f;
            Destroy(gameObject, destroyDelay);
        }


    }

    public enum BulletSource
    {
        Player, Enemy, Environment
    }
}