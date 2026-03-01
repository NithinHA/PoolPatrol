using DG.Tweening;
using UnityEngine;

namespace Player.Floatie
{
    public class FloatieController : MonoBehaviour
    {
        [SerializeField] private PlayerController m_Player;
        [SerializeField] private Transform m_GfxParent;
        [SerializeField] private Transform m_Gfx;
        [SerializeField] private Transform m_Reflection;

        [Header("Follow Settings")]
        [Tooltip("How quickly the floatie catches up to the player (lower = more lag).")]
        [SerializeField] private float m_FollowSmoothTime = 0.12f;
        [Tooltip("Max world-units the floatie can lag behind the player.")]
        [SerializeField] private float m_MaxLagDistance = 0.4f;

        [Header("Wiggle Settings")]
        [SerializeField] private float m_FireWiggleIntensity = 1f;
        [SerializeField] private float m_BounceWiggleIntensity = 0.4f;
        [SerializeField] private float m_WiggleDuration = 0.45f;

        [Header("Spin Settings")]
        [Tooltip("Degrees per second added to spin on a full-intensity wiggle.")]
        [SerializeField] private float m_SpinImpulse = 180f;
        [Tooltip("How quickly spin decays each second (0 = snap stop, 1 = no decay). ~0.04 gives a few-second spin.")]
        [SerializeField] private float m_AngularDrag = 0.04f;

        // Follow state
        private Vector2 _followVelocity;

        // Wiggle (scale) state
        private Sequence _wiggleSequence;
        private Vector3 _originalGfxParentScale;

        // Spin (rotation) state - driven in LateUpdate, independent of sequences
        private float _angularVelocity;  // degrees/sec, signed ('+' = CCW, '-' = CW)
        private float _currentRotationZ; // accumulated angle, never resets

#region Unity callbacks

        private void Awake()
        {
            _originalGfxParentScale = m_GfxParent.localScale;
        }

        private void Start()
        {
            if (m_Player == null)
                m_Player = FindFirstObjectByType<PlayerController>();

            transform.position = m_Player.transform.position;
            m_Player.OnFireInput += OnFireInput;
            m_Player.ImpulseMover.OnBounce += OnPlayerBounce;
        }

        private void OnDestroy()
        {
            m_Player.OnFireInput -= OnFireInput;
            m_Player.ImpulseMover.OnBounce -= OnPlayerBounce;
        }

        private void LateUpdate()
        {
            FollowPlayer();
            TickSpin();
        }

#endregion

        private void FollowPlayer()
        {
            Vector2 playerPos = m_Player.transform.position;
            Vector2 currentPos = transform.position;

            // SmoothDamp gives organic, momentum-like lag.
            Vector2 newPos = Vector2.SmoothDamp(currentPos, playerPos, ref _followVelocity, m_FollowSmoothTime);

            // Hard clamp: floatie must never drift so far that the player exits its bounds.
            if (Vector2.Distance(newPos, playerPos) > m_MaxLagDistance)
                newPos = playerPos + (newPos - playerPos).normalized * m_MaxLagDistance;

            transform.position = newPos;
        }

        /// <summary>
        /// Runs every LateUpdate. Integrates angular velocity into the accumulated rotation,
        /// Then decays velocity exponentially.
        /// </summary>
        private void TickSpin()
        {
            if (Mathf.Approximately(_angularVelocity, 0f))
                return;

            _currentRotationZ += _angularVelocity * Time.deltaTime;

            Quaternion rot = Quaternion.Euler(0f, 0f, _currentRotationZ);
            m_Gfx.localRotation = rot;
            m_Reflection.localRotation = rot;

            // Exponential decay: v *= drag^dt  → frame-rate independent.
            _angularVelocity *= Mathf.Pow(m_AngularDrag, Time.deltaTime);

            // Snap to zero once negligible (prevents infinite micro-updates).
            if (Mathf.Abs(_angularVelocity) < 0.5f)
                _angularVelocity = 0f;
        }

        /// <summary>
        /// Stretch+squish Parent along 'direction', then ease back.
        /// </summary>
        private void Wiggle(float intensity, Vector2 direction)
        {
            // kill existing tween and reset
            _wiggleSequence?.Kill(complete: false);
            m_GfxParent.localScale = _originalGfxParentScale;

            // if direction is more Y than X => Vertical stretch; else Horizontal stretch.
            bool isVertical = Mathf.Abs(direction.y) >= Mathf.Abs(direction.x);
            float stretch = 1f + 0.35f * intensity;
            float squish  = 1f - 0.20f * intensity;

            Vector3 stretchedScaleParent = isVertical
                ? new Vector3(_originalGfxParentScale.x * squish,  _originalGfxParentScale.y * stretch, _originalGfxParentScale.z)
                : new Vector3(_originalGfxParentScale.x * stretch, _originalGfxParentScale.y * squish,  _originalGfxParentScale.z);

            float halfDuration = m_WiggleDuration * 0.5f;
            _wiggleSequence = DOTween.Sequence();
            _wiggleSequence.Join(m_GfxParent.DOScale(stretchedScaleParent, halfDuration * 0.4f).SetEase(Ease.OutQuad));
            _wiggleSequence.Append(m_GfxParent.DOScale(_originalGfxParentScale, m_WiggleDuration * 0.6f).SetEase(Ease.OutElastic));
            _wiggleSequence.SetAutoKill(true);
        }

#region Event listeners

        private void OnFireInput(Vector2 fireDir, Vector2 worldPos)
        {
            Wiggle(m_FireWiggleIntensity, fireDir);

            // Perform rotation; Compute rotation direction (right->CW; left->CCW) and stack angular velocity. 
            float rotSign = fireDir.x >= 0 ? -1f : 1f;
            _angularVelocity += m_SpinImpulse * m_FireWiggleIntensity * rotSign;
        }

        private void OnPlayerBounce(Vector2 direction)
        {
            Wiggle(m_BounceWiggleIntensity, direction);
        }

#endregion
    }
}
