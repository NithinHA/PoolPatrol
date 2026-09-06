using DG.Tweening;
using UnityEngine;

namespace UI.HUD
{
    /// <summary>
    /// One checkpoint on the <see cref="ArenaProgressBar"/> marking where a wave section sits on the
    /// timeline. Sits as a dot while pending, pulsates while its wave is in progress, and swaps to a
    /// checkmark once the wave is cleared.
    /// </summary>
    public class WaveMarker : MonoBehaviour
    {
        [SerializeField] private GameObject m_DotVisual;
        [SerializeField] private GameObject m_CheckVisual;
        [SerializeField] private float m_PulseScale = 1.15f;
        [SerializeField] private float m_PulseDuration = 0.6f;

        private RectTransform _rect;
        private Tween _pulseTween;

        private void Awake() => _rect = GetComponent<RectTransform>();

        /// <summary>Wave not yet reached, or reached but not yet started: static dot.</summary>
        public void SetPending()
        {
            StopPulsing();
            if (m_DotVisual != null) m_DotVisual.SetActive(true);
            if (m_CheckVisual != null) m_CheckVisual.SetActive(false);
        }

        /// <summary>Wave currently in progress: dot pulsates gently to draw the eye.</summary>
        public void SetActivePulsing()
        {
            if (m_DotVisual != null) m_DotVisual.SetActive(true);
            if (m_CheckVisual != null) m_CheckVisual.SetActive(false);

            StopPulsing();
            if (_rect != null)
                _pulseTween = _rect.DOScale(m_PulseScale, m_PulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
        }

        /// <summary>Wave cleared: dot is replaced by a checkmark.</summary>
        public void SetCleared()
        {
            StopPulsing();
            if (m_DotVisual != null) m_DotVisual.SetActive(false);
            if (m_CheckVisual != null) m_CheckVisual.SetActive(true);
        }

        private void StopPulsing()
        {
            _pulseTween?.Kill();
            _pulseTween = null;
            if (_rect != null) _rect.localScale = Vector3.one;
        }

        private void OnDestroy() => StopPulsing();
    }
}
