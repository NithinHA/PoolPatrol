using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    /// <summary>
    /// Manages the visual state of a single heart icon in the LivesPanel.
    /// Driven entirely by LivesPanel — holds no game-state references.
    /// </summary>
    public class HeartIcon : MonoBehaviour
    {
        [SerializeField] private Image m_Image;
        [SerializeField] private float m_AnimDuration = 0.25f;

        private Vector3 _originalScale;
        private Tween   _activeTween;

        private void Awake()
        {
            if (m_Image == null)
                m_Image = GetComponentInChildren<Image>();

            _originalScale = transform.localScale;
        }

        /// <summary>Pop the heart into view (called when health increases or on init).</summary>
        public void Appear(bool animate = true)
        {
            gameObject.SetActive(true);

            _activeTween?.Kill();
            if (animate)
            {
                transform.localScale = Vector3.zero;
                _activeTween = transform.DOScale(_originalScale, m_AnimDuration).SetEase(Ease.OutBack);
            }
            else
            {
                transform.localScale = _originalScale;
            }
        }

        /// <summary>Shrink the heart out of view (called on damage).</summary>
        public void Disappear(bool animate = true)
        {
            _activeTween?.Kill();
            if (animate)
            {
                _activeTween = transform.DOScale(Vector3.zero, m_AnimDuration)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                transform.localScale = _originalScale;
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy() => _activeTween?.Kill();
    }
}
