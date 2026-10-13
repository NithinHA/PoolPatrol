using System;
using Abilities;
using DG.Tweening;
using Player;
using Pooling;
using PTL.Framework;
using PTL.Framework.Services;
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
    /// Items left alone do not linger: an idle item floats for <c>IdleLifetime</c> seconds and then
    /// sinks out of the pool, lost. That clock only runs in <see cref="State.Idle"/> — once the
    /// player is close enough to start the item homing it is safe, because homing is a one-way door
    /// (an item that has begun travelling to the player is about to be collected).
    ///
    /// Every instance is pooled through <see cref="ObjectPoolManager"/>, so all per-drop state is
    /// reset in <see cref="OnEnable"/> rather than Awake — see the note there.
    ///
    /// Subclasses supply only the grant action and the UI destination.
    /// </summary>
    public abstract class CollectableBase : MonoBehaviour, IPoolableObject
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

        [Header("Lifetime")]
        [Tooltip("Seconds the item floats before it sinks and is lost. Only counts down while idle — " +
                 "an item already homing toward the player never sinks.")]
        [SerializeField] private float m_IdleLifetime = 15f;

        [Tooltip("How long before sinking the item starts flashing to warn the player. 0 disables it.")]
        [SerializeField] private float m_SinkWarningTime = 3f;

        [Header("Sink")]
        [SerializeField] private float m_SinkDuration = 0.7f;

        [Tooltip("How far the item drifts down as it sinks out of sight.")]
        [SerializeField] private float m_SinkDepth = 0.6f;

        [Tooltip("Scale the item shrinks to as it sinks away from the surface.")]
        [SerializeField] private float m_SinkScale = 0.45f;

        [Tooltip("Sprite that fades out on sinking / flashes during the warning.")]
        [SerializeField] private SpriteRenderer m_Gfx;

        /// <summary>
        /// Fired the instant an item is granted (before it flies to the HUD).
        /// Player and UI can subscribe for extra feedback (SFX, popups, etc.).
        /// </summary>
        public static event Action<CollectableBase> OnAnyCollected;

        /// <summary>UI destination this item flies toward after being granted.</summary>
        protected abstract CollectionTargetId TargetId { get; }

        /// <summary>
        /// Run-modifier stat that widens this item's outer collection radius, or null when the
        /// item is not magnetisable. <see cref="GemCollectable"/> returns
        /// <see cref="StatId.GemMagnetRadius"/> so the "Gem Magnet" ability actually reaches here.
        /// </summary>
        protected virtual StatId? MagnetRadiusStat => null;

        /// <summary>Applies the actual reward (economy grant, heal, …). Called once at grant.</summary>
        protected abstract void Grant();

        /// <summary>
        /// <c>Collected</c> and <c>Sinking</c> are both terminal and mutually exclusive, which is
        /// what guarantees exactly one <see cref="Despawn"/> per drop — the pool throws if the same
        /// instance is released twice.
        /// </summary>
        private enum State { Idle, Homing, Sinking, Collected }

        private State _state = State.Idle;
        private Camera _camera;
        private IRunModifierService _modifiers;
        private float _effectiveCollectionRadius;
        private float _idleTime;

        private Tween _warningTween;
        private Sequence _sinkSequence;

        private PoolableItemType _itemType;

        protected virtual void Awake()
        {
            _camera = Camera.main;

            if (m_Gfx == null)
                Debug.LogError($"[{GetType().Name}] No gfx SpriteRenderer assigned — this item cannot fade or flash.", this);
        }

        /// <summary>
        /// Runtime setup lives in OnEnable/OnDisable rather than Start/OnDestroy, so this component
        /// works correctly both the normal way (Instantiate/Destroy) and once these are pooled
        /// (SetActive toggling, same instance reused for a later drop): OnEnable re-runs every time
        /// an instance comes back into play and must reset all per-collection state itself, since
        /// Awake will not run again for a reused pooled instance.
        /// </summary>
        protected virtual void OnEnable()
        {
            _state = State.Idle;
            _effectiveCollectionRadius = m_CollectionRadius;
            _idleTime = 0f;
            transform.localScale = Vector3.one;
            SetGfxAlpha(1f);

            if (MagnetRadiusStat == null)
                return;

            _modifiers ??= ServiceLocator.GetRunModifierService();
            if (_modifiers == null)
                return;

            // Re-read on change so a magnet bought mid-flight widens items already on the ground.
            _modifiers.OnModifiersChanged += RefreshCollectionRadius;
            RefreshCollectionRadius();
        }

        protected virtual void OnDisable()
        {
            if (_modifiers != null)
                _modifiers.OnModifiersChanged -= RefreshCollectionRadius;

            KillSinkTweens();
            transform.DOKill();
        }

        private void KillSinkTweens()
        {
            _warningTween?.Kill();
            _warningTween = null;
            _sinkSequence?.Kill();
            _sinkSequence = null;
        }

        /// <summary>Recomputes the outer radius from the inspector value plus any magnet bonus.</summary>
        private void RefreshCollectionRadius()
        {
            _effectiveCollectionRadius = MagnetRadiusStat is { } stat && _modifiers != null
                ? m_CollectionRadius + _modifiers.GetFlatSum(stat)
                : m_CollectionRadius;
        }

        protected virtual void Update()
        {
            switch (_state)
            {
                case State.Idle:
                    TickIdle();
                    break;

                case State.Homing:
                    TickHoming();
                    break;
            }
        }

        /// <summary>
        /// Floating on the surface: either the player comes close enough to pull the item in, or the
        /// lifetime runs out and it sinks. The player always wins a tie — the pickup check runs first.
        /// </summary>
        private void TickIdle()
        {
            _idleTime += Time.deltaTime;

            PlayerController local = PlayerController.Local;
            if (local != null &&
                SqrDistanceTo(local) <= _effectiveCollectionRadius * _effectiveCollectionRadius)
            {
                BeginHoming();
                return;
            }

            if (_idleTime >= m_IdleLifetime)
            {
                BeginSink();
                return;
            }

            if (_warningTween == null && m_SinkWarningTime > 0f &&
                _idleTime >= m_IdleLifetime - m_SinkWarningTime)
                StartSinkWarning();
        }

        private void TickHoming()
        {
            PlayerController local = PlayerController.Local;
            if (local == null)
                return;

            transform.position = Vector3.MoveTowards(
                transform.position, local.transform.position, m_HomingSpeed * Time.deltaTime);

            if (SqrDistanceTo(local) <= m_GrantRadius * m_GrantRadius)
                Collect();
        }

        private float SqrDistanceTo(PlayerController local) =>
            ((Vector2)transform.position - (Vector2)local.transform.position).sqrMagnitude;

        /// <summary>
        /// Entering homing cancels the sink clock for good: the item is on its way to the player.
        /// </summary>
        private void BeginHoming()
        {
            _state = State.Homing;
            KillSinkTweens();
            SetGfxAlpha(1f);
        }

        /// <summary>Flashes the sprite so the player can see the item is about to be lost.</summary>
        private void StartSinkWarning()
        {
            _warningTween = m_Gfx.DOFade(0.25f, 0.25f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// The item slips under the surface: it drifts down, shrinks away with depth and fades out,
        /// then despawns ungranted. Terminal — it can no longer be picked up.
        /// </summary>
        private void BeginSink()
        {
            _state = State.Sinking;
            KillSinkTweens();

            _sinkSequence = DOTween.Sequence();
            _sinkSequence.Join(transform
                .DOMoveY(transform.position.y - m_SinkDepth, m_SinkDuration)
                .SetEase(Ease.InQuad));
            _sinkSequence.Join(transform
                .DOScale(Vector3.one * m_SinkScale, m_SinkDuration)
                .SetEase(Ease.InQuad));
            _sinkSequence.Join(m_Gfx.DOFade(0f, m_SinkDuration).SetEase(Ease.InQuad));
            _sinkSequence.OnComplete(Despawn);
        }

        private void SetGfxAlpha(float alpha)
        {
            Color color = m_Gfx.color;
            color.a = alpha;
            m_Gfx.color = color;
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
            ReturnToPool();
        }

#region IPoolableObject

        public void Initialize(PoolableItemType type)
        {
            _itemType = type;
        }

        /// <summary>
        /// Nothing to do here: the pool activates the object before calling this, and that activation
        /// is what runs <see cref="OnEnable"/>, which already restores every per-drop field.
        /// </summary>
        public void Reset() { }

        public void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReleaseItem(_itemType, this);
        }

#endregion

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position,
                Application.isPlaying ? _effectiveCollectionRadius : m_CollectionRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, m_GrantRadius);
        }
    }
}
