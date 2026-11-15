using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    /// <summary>
    /// One checkpoint on the <see cref="ArenaProgressBar"/> marking where a wave section sits on the
    /// timeline. Sits as an empty slot while pending, pulsates while its wave is in progress, and
    /// swaps to a checkmark once the wave is cleared.
    ///
    /// Clearing is a short, deliberately quiet beat: the marker squashes and pops back, the check
    /// stamps in with a small overshoot and twist, and a single water ripple rings out behind it.
    /// The ripple Image is disabled when idle so it costs no draw call.
    /// </summary>
    public class WaveMarker : MonoBehaviour
    {
        [SerializeField] private GameObject m_DotVisual;
        [SerializeField] private GameObject m_CheckVisual;
        [SerializeField] private float m_PulseScale = 1.15f;
        [SerializeField] private float m_PulseDuration = 0.6f;

        [Header("Cleared animation (optional parts)")]
        [Tooltip("The tick inside the check visual; stamped in separately from its backing.")]
        [SerializeField] private RectTransform m_CheckIcon;
        [Tooltip("Ripple ring that expands and fades behind the marker.")]
        [SerializeField] private Image m_ClearRing;
        [SerializeField] private float m_ClearPopScale = 1.35f;
        [SerializeField] private float m_ClearDuration = 0.4f;
        [SerializeField] private float m_CheckTwist = 25f;
        [SerializeField] private float m_RingStartScale = 0.8f;
        [SerializeField] private float m_RingEndScale = 2.4f;
        [SerializeField] private float m_RingDuration = 0.6f;
        [SerializeField, Range(0f, 1f)] private float m_RingAlpha = 0.9f;

        private RectTransform _rect;
        private Tween _pulseTween;
        private Tween _clearTween;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (m_ClearRing != null)
            {
                m_ClearRing.raycastTarget = false;
                m_ClearRing.gameObject.SetActive(false);
            }
        }

        /// <summary>Wave not yet reached, or reached but not yet started: static slot.</summary>
        public void SetPending()
        {
            StopAnimations();
            if (m_DotVisual != null) m_DotVisual.SetActive(true);
            if (m_CheckVisual != null) m_CheckVisual.SetActive(false);
        }

        /// <summary>Wave currently in progress: slot pulsates gently to draw the eye.</summary>
        public void SetActivePulsing()
        {
            if (m_DotVisual != null) m_DotVisual.SetActive(true);
            if (m_CheckVisual != null) m_CheckVisual.SetActive(false);

            StopAnimations();
            if (_rect != null)
                _pulseTween = _rect.DOScale(m_PulseScale, m_PulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
        }

        /// <summary>Wave cleared: slot is replaced by a checkmark, with a small celebratory pop.</summary>
        public void SetCleared(bool animate = true)
        {
            StopAnimations();
            if (m_DotVisual != null) m_DotVisual.SetActive(false);
            if (m_CheckVisual != null) m_CheckVisual.SetActive(true);

            if (animate && _rect != null)
                PlayClearAnimation();
        }

        private void PlayClearAnimation()
        {
            float half = m_ClearDuration * 0.5f;
            Sequence seq = DOTween.Sequence();

            // Squash down, then pop past full size and settle.
            _rect.localScale = Vector3.one * 0.6f;
            seq.Append(_rect.DOScale(m_ClearPopScale, half).SetEase(Ease.OutQuad));
            seq.Append(_rect.DOScale(1f, half).SetEase(Ease.OutBack));

            if (m_CheckIcon != null)
            {
                m_CheckIcon.localScale = Vector3.zero;
                m_CheckIcon.localEulerAngles = new Vector3(0f, 0f, m_CheckTwist);
                seq.Insert(half * 0.5f, m_CheckIcon.DOScale(1f, m_ClearDuration).SetEase(Ease.OutBack, 2.2f));
                seq.Insert(half * 0.5f, m_CheckIcon.DOLocalRotate(Vector3.zero, m_ClearDuration).SetEase(Ease.OutBack));
            }

            if (m_ClearRing != null)
            {
                RectTransform ringRect = m_ClearRing.rectTransform;
                m_ClearRing.gameObject.SetActive(true);
                ringRect.localScale = Vector3.one * m_RingStartScale;
                Color c = m_ClearRing.color;
                c.a = m_RingAlpha;
                m_ClearRing.color = c;

                seq.Insert(0f, ringRect.DOScale(m_RingEndScale, m_RingDuration).SetEase(Ease.OutCubic));
                seq.Insert(0f, m_ClearRing.DOFade(0f, m_RingDuration).SetEase(Ease.InQuad));
            }

            seq.OnComplete(ResetClearParts);
            _clearTween = seq;
        }

        private void ResetClearParts()
        {
            if (m_ClearRing != null) m_ClearRing.gameObject.SetActive(false);
            if (m_CheckIcon != null)
            {
                m_CheckIcon.localScale = Vector3.one;
                m_CheckIcon.localEulerAngles = Vector3.zero;
            }
        }

        private void StopAnimations()
        {
            _pulseTween?.Kill();
            _pulseTween = null;

            if (_clearTween != null)
            {
                _clearTween.Kill();
                _clearTween = null;
                ResetClearParts();
            }

            if (_rect != null) _rect.localScale = Vector3.one;
        }

        private void OnDestroy() => StopAnimations();
    }
}
