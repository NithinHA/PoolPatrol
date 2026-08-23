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
        [Space]
        [SerializeField] private Color m_ActiveColor = Color.yellow;
        [SerializeField] private Color m_InactiveColor = Color.gray;

        public bool IsActive { get; private set; }

        private Tween _activeTween;

        private void Awake()
        {
            if (m_Image == null)
                m_Image = GetComponentInChildren<Image>();
        }

        /// <summary>Switch to the active (full-health) color.</summary>
        public void Appear(bool animate = true)
        {
            _activeTween?.Kill();
            if (animate)
                _activeTween = m_Image.DOColor(m_ActiveColor, m_AnimDuration).SetEase(Ease.OutQuad);
            else
                m_Image.color = m_ActiveColor;

            IsActive = true;
        }

        /// <summary>Switch to the inactive (lost-health) color.</summary>
        public void Disappear(bool animate = true)
        {
            _activeTween?.Kill();
            if (animate)
                _activeTween = m_Image.DOColor(m_InactiveColor, m_AnimDuration).SetEase(Ease.InQuad);
            else
                m_Image.color = m_InactiveColor;

            IsActive = false;
        }

        private void OnDestroy() => _activeTween?.Kill();
    }
}
