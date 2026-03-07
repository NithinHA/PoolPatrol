using Enemy;
using Enemy.Death;
using Player;
using Pooling;
using UnityEngine;

namespace Weapon
{
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

        public void Fire(Vector2 direction, float speed, BulletSource source, WeaponAttack attack, float range, Vector3 startPosition)
        {
            _source = source;
            _attack = attack;
            _startPosition = startPosition;
            _sqrRange = range * range;
            direction.Normalize();
            m_Rb2D.AddForce(direction * speed, ForceMode2D.Impulse);
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