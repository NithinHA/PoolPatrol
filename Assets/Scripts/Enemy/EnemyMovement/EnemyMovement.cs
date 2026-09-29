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

        protected Vector2 MoveDirection;
        private Tween _rotationTween;

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

        public void PushBack(float pushBackForce, Vector2 direction)
        {
            Controller.RigidBody.AddForce(direction * pushBackForce, ForceMode2D.Impulse);
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