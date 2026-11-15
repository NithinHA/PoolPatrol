using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Weapon;

namespace UI.HUD
{
    /// <summary>
    /// Single ammo indicator for WeaponIndicator (new). The root Image is the empty socket (what a
    /// spent round looks like); the bullet_fill child is the live round, whose fill amount represents
    /// load progress. Firing plays a short beat: the round flashes white and pops up in size, then
    /// shrinks back while dimming to grey and fading out, leaving the empty socket.
    ///
    /// Everything is a handful of DOTween tweens on existing RectTransforms — no particle systems.
    /// </summary>
    public class BulletIcon : MonoBehaviour
    {
        [SerializeField] private Image m_BulletFill;
        [SerializeField] private float m_SpentScale = 0.67f;

        [Header("Colors")]
        [SerializeField] private Color m_LiveColor = Color.white;
        [SerializeField] private Color m_FlashColor = Color.white;
        [SerializeField] private Color m_SpentColor = new Color(0.45f, 0.48f, 0.55f, 1f);

        [Header("Fire")]
        [Tooltip("Scale the round pops to on the flash, before it shrinks into a spent casing.")]
        [SerializeField] private float m_FirePopScale = 1.75f;
        [SerializeField] private float m_FirePopDuration = 0.07f;
        [Tooltip("Pop → grey. The socket underneath is what remains.")]
        [SerializeField] private float m_FireDimDuration = 0.2f;
        [SerializeField] private float m_FireFadeDuration = 0.1f;

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
        private Tween _fireTween;

        private void Awake()
        {
            _fillRect = m_BulletFill.rectTransform;
        }

        /// <summary>
        /// Bullet is loaded and ready to fire.
        /// </summary>
        public void SetLive()
        {
            KillRoundTweens();
            m_BulletFill.fillAmount = 1f;
            m_BulletFill.color = m_LiveColor;
            _fillRect.localScale = Vector3.one;
        }

        /// <summary>
        /// Shows the slot as empty with no animation — initial build and bookkeeping resets.
        /// For the shot itself use <see cref="Fire"/>.
        /// </summary>
        public void SetSpent()
        {
            KillRoundTweens();
            m_BulletFill.fillAmount = 0f;
            m_BulletFill.color = m_LiveColor;
            _fillRect.localScale = Vector3.one * m_SpentScale;
        }

        /// <summary>
        /// This round was just fired: flash white, pop big, then shrink and grey out to the empty socket.
        /// </summary>
        public void Fire()
        {
            KillRoundTweens();

            m_BulletFill.fillAmount = 1f;
            m_BulletFill.color = m_FlashColor;
            _fillRect.localScale = Vector3.one;

            Color spentClear = m_SpentColor;
            spentClear.a = 0f;

            Sequence seq = DOTween.Sequence();
            seq.Append(_fillRect.DOScale(m_FirePopScale, m_FirePopDuration).SetEase(Ease.OutQuad));
            seq.Append(_fillRect.DOScale(m_SpentScale, m_FireDimDuration).SetEase(Ease.InQuad));
            seq.Join(m_BulletFill.DOColor(m_SpentColor, m_FireDimDuration).SetEase(Ease.OutQuad));
            seq.Append(m_BulletFill.DOColor(spentClear, m_FireFadeDuration).SetEase(Ease.InQuad));
            seq.OnComplete(SetSpent);
            _fireTween = seq;
        }

        /// <summary>
        /// Begins the reload animation state: fill is driven by WeaponPanel via
        /// SetReloadProgress(t), while the icon gently pulsates to read as
        /// "charging" until the reload completes.
        /// </summary>
        public void BeginReload(ReloadStyle style)
        {
            KillRoundTweens();
            m_BulletFill.color = m_LiveColor;

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
            KillRoundTweens();
            m_BulletFill.fillAmount = 1f;
            m_BulletFill.color = m_LiveColor;
            _fillRect.localScale = Vector3.one * m_SpentScale;

            Sequence pop = DOTween.Sequence();
            pop.Append(_fillRect.DOScale(Vector3.one * m_ReloadCompleteOvershoot, m_ReloadCompleteScaleDuration * 0.5f)
                .SetEase(Ease.OutCubic));
            pop.Append(_fillRect.DOScale(Vector3.one, m_ReloadCompleteScaleDuration * 0.5f)
                .SetEase(m_EaseMode));
            _scaleTween = pop;
        }

        private void KillRoundTweens()
        {
            _scaleTween?.Kill();
            _pulseTween?.Kill();
            _fireTween?.Kill();
            _scaleTween = _pulseTween = _fireTween = null;
        }

        private void OnDestroy() => KillRoundTweens();
    }
}
