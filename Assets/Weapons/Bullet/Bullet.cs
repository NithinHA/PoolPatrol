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

        private WeaponControllerBase _heldWeaponController;

        private WaitForSeconds _outOfBoundDelay = new WaitForSeconds(1f);
        private Vector3 _lastFrameVelocity;
        
        public Action<BulletSource> OnBulletImpact;
        public Action OnBulletDestroy;

        private bool _isBulletHitSuccess = false;

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
                _heldWeaponController.OnBulletDestroyed?.Invoke(this);
                OnBulletDestroy?.Invoke();
            }
            else if (other.gameObject.CompareTag(Constants.GameConstants.TAG_Player))
            {
                PlayerController player = other.gameObject.GetComponent<PlayerController>();
                if(_heldWeaponController.BulletSource == BulletSource.Enemy)
                {
                    player.PlayerHealth.TakeDamage();
                    _heldWeaponController.OnHitSuccess?.Invoke(transform.position);
                    OnBulletImpact?.Invoke(_heldWeaponController.BulletSource);
                    _isBulletHitSuccess = true;
                }
                DestroyBullet();
            }
            else if (other.gameObject.CompareTag(Constants.GameConstants.TAG_Enemy))
            {
                EnemyController enemy = other.gameObject.GetComponent<EnemyController>();
                if (_heldWeaponController.BulletSource == BulletSource.Player || _heldWeaponController.BulletSource == BulletSource.Environment)
                {
                    Dictionary<string, object> parameters = new Dictionary<string, object>()
                    {
                        { Constants.GameConstants.BULLET_COLLISION_Collider, other.gameObject },
                        { Constants.GameConstants.BULLET_COLLISION_Direction, _lastFrameVelocity.normalized }
                    };
                    enemy.Die(parameters);
                    _heldWeaponController.OnHitSuccess?.Invoke(transform.position);
                    OnBulletImpact?.Invoke(_heldWeaponController.BulletSource);
                    _isBulletHitSuccess = true;
                    OnHitEnemyWithCombo();
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
        
        public void Fire(Vector2 direction, float speed, WeaponControllerBase weaponController)
        {
            _heldWeaponController = weaponController;
            direction.Normalize();
            m_Rb2D.AddForce(direction * speed, ForceMode2D.Impulse);
        }

        private IEnumerator OutOfBoundsCheckRoutine()
        {
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);

            if (viewportPos.x < -1 || viewportPos.x > 2 || 
                viewportPos.y < -1 || viewportPos.y > 2)
            {
                DestroyBullet(true);
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

        private void DestroyBullet(bool isOutOfBounds = false)
        {
            if(!_isBulletHitSuccess)
                _heldWeaponController.OnHitFail?.Invoke();

            if (!isOutOfBounds)
            {
                ObjectPoolManager.Instance.SpawnItem(PoolableItemType.BulletImpactParticles, transform.position, Quaternion.identity);
                _heldWeaponController.OnBulletDestroyed?.Invoke(this);
                OnBulletDestroy?.Invoke();
            }

            Destroy(this.gameObject);
        }

#region Handle Player Combo

        public void OnHitEnemyWithCombo()
        {
            // check the current player combo level. How can I get the player combo level at this point?
            // compute range based on the combo level: Low=0, Mid=1, High=1.4, Max=1.8  
            // check if any enemy exists inside circle of radius=range from this transform position.
            // call enemy.Die for all the enemies within the range
            // also spawn explosion prefab at this position and set scale = range
            // DestroyBullet gets called by the invoking function at this point. Hope this won't cause memory leaks and other issues.
        }

#endregion
    }

    public enum BulletSource
    {
        Player, Enemy, Environment
    }
}