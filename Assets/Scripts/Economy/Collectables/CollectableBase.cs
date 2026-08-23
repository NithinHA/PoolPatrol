using System;
using DG.Tweening;
using Player;
using UnityEngine;

namespace Economy
{
    /// <summary>
    /// Base for world-space pickups (gems, hearts) dropped in the arena.
    ///
    /// Two-phase collection:
    ///   1. The local player enters the outer <c>CollectionRadius</c> → the item homes toward them.
    ///   2. The item reaches the inner <c>GrantRadius</c> → <see cref="Grant"/> runs and
    ///      <see cref="OnAnyCollected"/> fires; the item then flies to its screen-space UI target
    ///      (<see cref="TargetId"/>) and despawns.
    ///
    /// Subclasses supply only the grant action and the UI destination.
    /// </summary>
    public abstract class CollectableBase : MonoBehaviour
    {
        [Header("Radii (world units)")]
        [Tooltip("Outer radius: player entering this begins the item homing toward them.")]
        [SerializeField] private float m_CollectionRadius = 3f;
        [Tooltip("Inner radius: reaching this grants the item.")]
        [SerializeField] private float m_GrantRadius = 0.4f;

        [Header("Movement")]
        [Tooltip("Homing speed toward the player (units/sec).")]
        [SerializeField] private float m_HomingSpeed = 12f;
        [Tooltip("Duration of the flight to the UI target after being granted.")]
        [SerializeField] private float m_FlyToUiDuration = 0.35f;
        [SerializeField] private Ease m_FlyToUiEase = Ease.InBack;

        /// <summary>
        /// Fired the instant an item is granted (before it flies to the HUD).
        /// Player and UI can subscribe for extra feedback (SFX, popups, etc.).
        /// </summary>
        public static event Action<CollectableBase> OnAnyCollected;

        /// <summary>UI destination this item flies toward after being granted.</summary>
        protected abstract CollectionTargetId TargetId { get; }

        /// <summary>Applies the actual reward (economy grant, heal, …). Called once at grant.</summary>
        protected abstract void Grant();

        private enum State { Idle, Homing, Collected }

        private State _state = State.Idle;
        private Camera _camera;

        protected virtual void Awake()
        {
            _camera = Camera.main;
        }

        protected virtual void Update()
        {
            if (_state == State.Collected)
                return;

            PlayerController local = PlayerController.Local;
            if (local == null)
                return;

            Vector2 playerPos = local.transform.position;
            float sqrDistance = ((Vector2)transform.position - playerPos).sqrMagnitude;

            switch (_state)
            {
                case State.Idle:
                    if (sqrDistance <= m_CollectionRadius * m_CollectionRadius)
                        _state = State.Homing;
                    break;

                case State.Homing:
                    transform.position = Vector3.MoveTowards(
                        transform.position, local.transform.position, m_HomingSpeed * Time.deltaTime);
                    if (sqrDistance <= m_GrantRadius * m_GrantRadius)
                        Collect();
                    break;
            }
        }

        protected virtual void OnDestroy()
        {
            transform.DOKill();
        }

        private void Collect()
        {
            _state = State.Collected;

            Grant();                        // subclass reward
            OnAnyCollected?.Invoke(this);   // player / UI hooks

            FlyToUiAndDespawn();
        }

        private void FlyToUiAndDespawn()
        {
            if (CollectionTargets.TryGetWorldPosition(TargetId, _camera, out Vector3 targetWorld))
            {
                transform.DOMove(targetWorld, m_FlyToUiDuration).SetEase(m_FlyToUiEase);
                transform.DOScale(Vector3.zero, m_FlyToUiDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(Despawn);
            }
            else
            {
                // No HUD target registered — grant already happened, just clean up.
                Despawn();
            }
        }

        protected virtual void Despawn()
        {
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_CollectionRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, m_GrantRadius);
        }
    }
}
