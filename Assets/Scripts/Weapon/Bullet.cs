using System.Collections;
using System.Collections.Generic;
using Enemy;
using Player;
using UnityEngine;

namespace Weapon
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private ParticleSystem m_BulletImpactEffect;
        [SerializeField] private Rigidbody2D m_Rb2D;
        [SerializeField] private TrailRenderer m_TrailRenderer;

        public Color BulletColor;
        private BulletSource _bulletSource;
        public BulletSource BulletSource => _bulletSource;

        private WaitForSeconds _outOfBoundDelay = new WaitForSeconds(1f);
        private Vector3 _lastFrameVelocity;

#region Unity callbacks

        private void Start()
        {
            StartCoroutine(OutOfBoundsCheckRoutine());
        }

        private void FixedUpdate()
        {
            _lastFrameVelocity = m_Rb2D.linearVelocity;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag(Constants.GameConstants.TAG_Player))
            {
                PlayerController player = other.gameObject.GetComponent<PlayerController>();
                if(BulletSource == BulletSource.Enemy)
                {
                    player.PlayerHealth.TakeDamage();
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
                        { Constants.GameConstants.BULLET_COLLISION_Collider, other },
                        { Constants.GameConstants.BULLET_COLLISION_Direction, _lastFrameVelocity.normalized }
                    };
                    enemy.Die(parameters);
                }
                DestroyBullet();
            }
            else
            {
                DestroyBullet();    // destroys on collision with anything!
            }
        }

#endregion

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
                Destroy(this.gameObject);
                yield break;
            }

            yield return _outOfBoundDelay;
            StartCoroutine(OutOfBoundsCheckRoutine());
        }

        public void SetTrailSize(float multiplier)
        {
            float currentSize = m_TrailRenderer.startWidth;
            m_TrailRenderer.startWidth = m_TrailRenderer.endWidth = currentSize * multiplier;
        }

        public void SetTrailColor(Color color)
        {
            m_TrailRenderer.startColor = color;
        }

        public void DestroyBullet()
        {
            Instantiate(m_BulletImpactEffect, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }
    }

    public enum BulletSource
    {
        Player, Enemy, Environment
    }
}