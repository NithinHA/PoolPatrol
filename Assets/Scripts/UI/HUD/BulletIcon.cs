using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Weapon;

namespace UI.HUD
{
    /// <summary>
    /// Single ammo indicator for WeaponIndicator (new). Drives the bullet_fill
    /// child: fill amount represents load progress, scale/punch/pulse effects
    /// sell the shoot and reload beats.
    /// </summary>
    public class BulletIcon : MonoBehaviour
    {
        [SerializeField] private Image m_BulletFill;
        [SerializeField] private float m_SpentScale = 0.67f;
        [Header("Shoot punch")]
        [SerializeField] private float m_ShootPunchScale = 0.35f;
        [SerializeField] private float m_ShootPunchDuration = 0.25f;
        [SerializeField] private int   m_ShootPunchVibrato = 8;
        [SerializeField] private float m_ShootPunchElasticity = 0.8f;
        [Header("Reload pulse (looping while a bullet is loading)")]
        [SerializeField] private float m_ReloadPulseScale = 0.12f;
        [SerializeField] private float m_ReloadPulseDuration = 0.35f;
        [Header("Reload complete pop")]
        [SerializeField] private float m_ReloadCompleteOvershoot = 1.4f;
        [SerializeField] private float m_ReloadCompleteScaleDuration = 0.35f;
        [SerializeField] private Ease  m_EaseMode = Ease.OutBack;

        private RectTransform _fillRect;
        private Tween _scaleTween;
        private Tween _pulseTween;

        private void Awake()
        {
            _fillRect = m_BulletFill.rectTransform;
        }

        /// <summary>
        /// Bullet is loaded and ready to fire.
        /// </summary>
        public void SetLive()
        {
            KillTweens();
            m_BulletFill.fillAmount = 1f;
            _fillRect.localScale = Vector3.one;
        }

        /// <summary>
        /// Called immediately after the shot fires. Fill drops instantly and
        /// the icon gives a punchy recoil kick for extra juice.
        /// </summary>
        public void SetSpent()
        {
            KillTweens();
            m_BulletFill.fillAmount = 0f;
            _fillRect.localScale = Vector3.one * m_SpentScale;
            _scaleTween = _fillRect.DOPunchScale(Vector3.one * m_ShootPunchScale, m_ShootPunchDuration,
                m_ShootPunchVibrato, m_ShootPunchElasticity);
        }

        /// <summary>
        /// Begins the reload animation state: fill is driven by WeaponPanel via
        /// SetReloadProgress(t), while the icon gently pulsates to read as
        /// "charging" until the reload completes.
        /// </summary>
        public void BeginReload(ReloadStyle style)
        {
            _scaleTween?.Kill();
            _pulseTween?.Kill();

            _fillRect.localScale = Vector3.one * m_SpentScale;
            _pulseTween = _fillRect
                .DOScale(Vector3.one * (m_SpentScale + m_ReloadPulseScale), m_ReloadPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// Sets the visual fill progress (0-1).
        /// Called by WeaponPanel.Update only while IsReloading is true.
        /// </summary>
        public void SetReloadProgress(float t)
        {
            m_BulletFill.fillAmount = t;
        }

        /// <summary>
        /// Reload for this bullet slot is done. Fill snaps to full and the
        /// bullet pops with a big overshoot before settling, so the moment
        /// reads clearly.
        /// </summary>
        public void CompleteReload()
        {
            m_BulletFill.fillAmount = 1f;

            _pulseTween?.Kill();
            _scaleTween?.Kill();
            _fillRect.localScale = Vector3.one * m_SpentScale;

            Sequence pop = DOTween.Sequence();
            pop.Append(_fillRect.DOScale(Vector3.one * m_ReloadCompleteOvershoot, m_ReloadCompleteScaleDuration * 0.5f)
                .SetEase(Ease.OutCubic));
            pop.Append(_fillRect.DOScale(Vector3.one, m_ReloadCompleteScaleDuration * 0.5f)
                .SetEase(m_EaseMode));
            _scaleTween = pop;
        }

        private void KillTweens()
        {
            _scaleTween?.Kill();
            _pulseTween?.Kill();
        }

        private void OnDestroy() => KillTweens();
    }
}
