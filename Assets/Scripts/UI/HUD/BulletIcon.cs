using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Weapon;

namespace UI.HUD
{
    public class BulletIcon : MonoBehaviour
    {
        [SerializeField] private Image m_BulletImage;
        [SerializeField] private Image m_ReloadFill;
        [Header("Colors")]
        [SerializeField] private Color m_LiveColor   = Color.white;
        [SerializeField] private Color m_SpentColor  = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        [Header("Animation")]
        [SerializeField] private float m_PopDuration = 0.15f;

        private Vector3 _originalScale;
        private Tween   _popTween;

        private void Awake()
        {
            _originalScale = transform.localScale;
            if (m_ReloadFill != null)
                m_ReloadFill.fillAmount = 0f;
        }

        /// <summary>
        /// Bullet is loaded and ready.
        /// </summary>
        public void SetLive()
        {
            m_BulletImage.color = m_LiveColor;
            SetReloadFillVisible(false);

            _popTween?.Kill();
            transform.localScale = _originalScale * 1.3f;
            _popTween = transform.DOScale(_originalScale, m_PopDuration).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// Called immediately after the shot fires.
        /// </summary>
        public void SetSpent()
        {
            m_BulletImage.color = m_SpentColor;
            SetReloadFillVisible(false);

            _popTween?.Kill();
            transform.localScale = _originalScale;
        }

        /// <summary>
        /// Begins the reload animation state.
        /// For both reload styles the fill is driven by WeaponPanel via SetReloadProgress(t).
        /// </summary>
        public void BeginReload(ReloadStyle style)
        {
            // Reset fill to empty; WeaponPanel will push progress each frame.
            if (m_ReloadFill != null)
            {
                m_ReloadFill.fillAmount = 0f;
                SetReloadFillVisible(true);
            }
        }

        /// <summary>
        /// Sets the visual fill progress (0–1).
        /// Called by WeaponPanel.Update only while IsReloading is true.
        /// </summary>
        public void SetReloadProgress(float t)
        {
            if (m_ReloadFill != null)
                m_ReloadFill.fillAmount = t;
        }

        /// <summary>
        /// Reload for this bullet slot is done. Clears the fill and calls SetLive.
        /// </summary>
        public void CompleteReload()
        {
            SetReloadFillVisible(false);
            SetLive();
        }

        private void SetReloadFillVisible(bool visible)
        {
            if (m_ReloadFill != null)
                m_ReloadFill.gameObject.SetActive(visible);
        }

        private void OnDestroy() => _popTween?.Kill();
    }
}
