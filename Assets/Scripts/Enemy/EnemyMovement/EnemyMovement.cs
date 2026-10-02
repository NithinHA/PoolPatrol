using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemy.Movement
{
    public abstract class EnemyMovement : EnemyComponentBase
    {
        [SerializeField] private float m_RotationSpeed = .6f;

        [Header("Directional sprites (optional)")]
        [Tooltip("If assigned, the enemy faces its move direction by swapping this renderer's sprite " +
                 "(8-way) instead of rotating its transform. Right-facing directions reuse the Left " +
                 "sprites mirrored via flipX.")]
        [SerializeField] private SpriteRenderer m_DirectionalRenderer;
        [SerializeField] private Sprite m_SpriteFront;   // moving down  (S)
        [SerializeField] private Sprite m_SpriteBack;    // moving up    (N)
        [SerializeField] private Sprite m_SpriteLeft45;  // moving down-left (SW)
        [SerializeField] private Sprite m_SpriteLeft90;  // moving left      (W)
        [SerializeField] private Sprite m_SpriteLeft135; // moving up-left   (NW)
        [Space]

        [Header("Knockback")]
        [Tooltip("How long an externally applied push (e.g. Damage Revenge) overrides this " +
                 "enemy's own movement before it resumes driving its own velocity.")]
        [SerializeField] private float m_KnockbackRecoveryTime = 0.3f;

        protected Vector2 MoveDirection;
        private Tween _rotationTween;
        private float _knockbackTimeRemaining;

#region Unity callbacks
        
        protected override void Awake()
        {
            base.Awake();
            SetRandomDirection();
            AdjustRotation();
        }

        protected virtual void OnDestroy()
        {
            if (_rotationTween != null && _rotationTween.IsActive() && _rotationTween.IsPlaying())
                _rotationTween.Kill();
        }

#endregion

        public abstract void Tick();
        public abstract void FixedTick();

        /// <summary>True while an external push is overriding this enemy's own movement logic.</summary>
        public bool IsKnockedBack => _knockbackTimeRemaining > 0f;

        /// <summary>
        /// Applies an external knockback (e.g. "Damage Revenge"). Sets velocity directly rather
        /// than <c>AddForce</c>, so it also works on Kinematic rigidbodies (idle-type enemies),
        /// which ignore forces entirely. Every concrete movement type drives its rigidbody's
        /// velocity itself each tick, so without the knockback window below that velocity would
        /// be overwritten within the same or next physics step — subclasses must call
        /// <see cref="TickKnockback"/> and skip their own velocity write while it returns true.
        /// </summary>
        public void PushBack(float pushBackForce, Vector2 direction)
        {
            if (Controller == null || Controller.RigidBody == null || direction.sqrMagnitude < 0.0001f)
                return;

            _knockbackTimeRemaining = m_KnockbackRecoveryTime;
            Controller.RigidBody.linearVelocity = direction.normalized * pushBackForce;
        }

        /// <summary>
        /// Call once per Tick/FixedTick (whichever drives this movement type's velocity) before
        /// deciding whether to write velocity normally. Returns true while knockback is still in
        /// effect. Zeroes the rigidbody's velocity the instant the window ends, so a movement type
        /// that doesn't touch velocity every frame (e.g. <c>EnemyNoMovement</c>) doesn't drift
        /// forever — a Kinematic/Dynamic body given a velocity has nothing else to stop it.
        /// </summary>
        protected bool TickKnockback(float dt)
        {
            if (_knockbackTimeRemaining <= 0f)
                return false;

            _knockbackTimeRemaining -= dt;
            if (_knockbackTimeRemaining <= 0f && Controller != null && Controller.RigidBody != null)
                Controller.RigidBody.linearVelocity = Vector2.zero;

            return true;
        }

        protected virtual void SetRandomDirection()
        {
            do
            {
                MoveDirection = Random.insideUnitCircle.normalized;
            } while (MoveDirection == Vector2.zero);
        }
        
        /// <summary>
        /// Updates the enemy's facing to match <see cref="MoveDirection"/>. When a directional
        /// renderer is assigned the transform stays upright and the sprite is swapped (8-way);
        /// otherwise the transform is smoothly rotated to face the direction (top-down style).
        /// </summary>
        protected void AdjustRotation()
        {
            if (m_DirectionalRenderer != null)
            {
                ApplyDirectionalSprite();
                return;
            }

            float targetAngle = Mathf.Atan2(MoveDirection.y, MoveDirection.x) * Mathf.Rad2Deg - 90f;

            // Kill existing tween if it's running
            if (_rotationTween != null && _rotationTween.IsActive() && _rotationTween.IsPlaying())
                _rotationTween.Kill();

            // Create and cache the new tween
            _rotationTween = transform.DORotate(new Vector3(0, 0, targetAngle), m_RotationSpeed)
                .SetEase(Ease.OutFlash);
        }

        /// <summary>
        /// Picks the 8-way sprite for the current <see cref="MoveDirection"/>. The circle is split
        /// into eight 45° octants CCW from +X (right). Right-side octants reuse the mirrored Left
        /// sprites via <see cref="SpriteRenderer.flipX"/>; Front/Back are symmetric so never flip.
        /// </summary>
        private void ApplyDirectionalSprite()
        {
            if (MoveDirection == Vector2.zero)
                return;

            int octant = Mathf.RoundToInt(Mathf.Atan2(MoveDirection.y, MoveDirection.x) * Mathf.Rad2Deg / 45f);
            if (octant < 0) octant += 8; // 0..7

            Sprite sprite;
            bool flip;
            switch (octant)
            {
                case 0: sprite = m_SpriteLeft90;  flip = true;  break; // right      (E)
                case 1: sprite = m_SpriteLeft135; flip = true;  break; // up-right   (NE)
                case 2: sprite = m_SpriteBack;    flip = false; break; // up         (N)
                case 3: sprite = m_SpriteLeft135; flip = false; break; // up-left    (NW)
                case 4: sprite = m_SpriteLeft90;  flip = false; break; // left       (W)
                case 5: sprite = m_SpriteLeft45;  flip = false; break; // down-left  (SW)
                case 6: sprite = m_SpriteFront;   flip = false; break; // down       (S)
                case 7: sprite = m_SpriteLeft45;  flip = true;  break; // down-right (SE)
                default: sprite = m_SpriteFront;  flip = false; break;
            }

            if (sprite != null)
                m_DirectionalRenderer.sprite = sprite;
            m_DirectionalRenderer.flipX = flip;
        }
    }
}