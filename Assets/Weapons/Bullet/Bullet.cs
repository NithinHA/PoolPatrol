using System;
using System.Collections;
using System.Collections.Generic;
using Enemy;
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

        private BulletSource _bulletSource;
        public BulletSource BulletSource => _bulletSource;

        private WaitForSeconds _outOfBoundDelay = new WaitForSeconds(1f);
        private Vector3 _lastFrameVelocity;
        
        public Action<BulletSource> OnBulletImpact;
        public Action OnBulletDestroy;

#region Unity callbacks

        private void Start()
        {
            StartCoroutine(OutOfBoundsCheckRoutine());
        }

        private void FixedUpdate()
        {
            _lastFrameVelocity = m_Rb2D.linearVelocity;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag(Constants.GameConstants.TAG_ScreenEdges))
            {
                ObjectPoolManager.Instance.SpawnItem(PoolableItemType.BulletImpactParticles, transform.position, Quaternion.identity);
                OnBulletDestroy?.Invoke();
            }
            else if (other.gameObject.CompareTag(Constants.GameConstants.TAG_Player))
            {
                PlayerController player = other.gameObject.GetComponent<PlayerController>();
                if(BulletSource == BulletSource.Enemy)
                {
                    player.PlayerHealth.TakeDamage();
                    OnBulletImpact?.Invoke(BulletSource);
                }
                DestroyBullet();
            }
            else if (other.gameObject.CompareTag(Constants.GameConstants.TAG_Enemy))
            {
                EnemyController enemy = other.gameObject.GetComponent<EnemyController>();
                if (BulletSource == BulletSource.Player || BulletSource == BulletSource.Environment)
                {
                    Dictionary<string, object> parameters = new Dictionary<string, object>()
                    {
                        { Constants.GameConstants.BULLET_COLLISION_Collider, other.gameObject },
                        { Constants.GameConstants.BULLET_COLLISION_Direction, _lastFrameVelocity.normalized }
                    };
                    enemy.Die(parameters);
                    OnBulletImpact?.Invoke(BulletSource);
                }
                DestroyBullet();
            }
            // else
            // {
            //     DestroyBullet();    // destroys on collision with anything!
            // }
        }

#endregion

        public void SetupScale(float sizeMultiplier, float colliderSizeMultiplier)
        {
            transform.localScale *= sizeMultiplier;
            SetTrailSize(sizeMultiplier);
            m_Collider.radius *= colliderSizeMultiplier;
        }
        
        public void Fire(Vector2 direction, float speed, BulletSource source)
        {
            _bulletSource = source;
            direction.Normalize();
            m_Rb2D.AddForce(direction * speed, ForceMode2D.Impulse);
        }

        private IEnumerator OutOfBoundsCheckRoutine()
        {
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);

            if (viewportPos.x < -1 || viewportPos.x > 2 || 
                viewportPos.y < -1 || viewportPos.y > 2)
            {
                DestroyBullet(false);
                yield break;
            }

            yield return _outOfBoundDelay;
            StartCoroutine(OutOfBoundsCheckRoutine());
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

        private void DestroyBullet(bool withParticles = true)
        {
            if (withParticles)
                ObjectPoolManager.Instance.SpawnItem(PoolableItemType.BulletImpactParticles, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
            OnBulletDestroy?.Invoke();
        }
    }

    public enum BulletSource
    {
        Player, Enemy, Environment
    }
}