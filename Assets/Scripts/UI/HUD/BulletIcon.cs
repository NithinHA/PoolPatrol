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

        [Header("Dry fire (trigger pulled on an empty magazine)")]
        [SerializeField] private float m_DryShakeStrength = 9f;
        [SerializeField] private float m_DryShakeDuration = 0.3f;
        [SerializeField] private int   m_DryShakeVibrato = 40;
        [Tooltip("Multiplied over the socket and round for an instant, then fades back to normal.")]
        [SerializeField] private Color m_DryTint = new Color(1f, 0.42f, 0.28f, 1f);

        private RectTransform _rect;
        private RectTransform _fillRect;
        private CanvasRenderer _socketRenderer;
        private CanvasRenderer _fillRenderer;

        private Tween _scaleTween;
        private Tween _pulseTween;
        private Tween _fireTween;
        private TweenCallback _onFireComplete;

        // Set when reload begins while the fire pop is still playing: the reload visual starts once
        // the pop finishes, rather than cutting it off mid-flash.
        private bool _reloadPending;

        private bool FirePlaying => _fireTween != null && _fireTween.IsActive();

        // Dry-fire flourish: independent of the round's state tweens, so it never interrupts a reload.
        private Tween _dryShakeTween;
        private Tween _dryTintTween;
        private float _dryTint;                       // 0 = normal, 1 = full tint
        private DG.Tweening.Core.DOGetter<float> _dryTintGetter;
        private DG.Tweening.Core.DOSetter<float> _dryTintSetter;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _fillRect = m_BulletFill.rectTransform;
            _socketRenderer = GetComponent<CanvasRenderer>();
            _fillRenderer = m_BulletFill.canvasRenderer;

            // Cached once: every dry fire reuses these instead of allocating closures.
            _dryTintGetter = () => _dryTint;
            _dryTintSetter = ApplyDryTint;
            _onFireComplete = OnFireComplete;
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
            seq.OnComplete(_onFireComplete);
            _fireTween = seq;
        }

        private void OnFireComplete()
        {
            _fireTween = null;
            bool reload = _reloadPending;
            SetSpent();
            if (reload)
                StartReloadVisual();
        }

        /// <summary>
        /// Begins the reload animation state: fill is driven by WeaponPanel via
        /// SetReloadProgress(t), while the icon gently pulsates to read as
        /// "charging" until the reload completes. A round still playing its fire pop finishes that
        /// first, then starts reloading.
        /// </summary>
        public void BeginReload(ReloadStyle style)
        {
            if (FirePlaying)
            {
                _reloadPending = true;
                return;
            }

            StartReloadVisual();
        }

        private void StartReloadVisual()
        {
            KillRoundTweens();
            m_BulletFill.color = m_LiveColor;
            // Every reload starts this slot from empty; WeaponPanel feeds the progress from here.
            // Without this, a slot whose fire pop was interrupted kept fillAmount = 1 and showed as
            // a full round until its turn to load came round.
            m_BulletFill.fillAmount = 0f;

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
            // The fire pop owns the fill until it finishes (see BeginReload).
            if (FirePlaying)
                return;
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

        /// <summary>
        /// The trigger was pulled with nothing to fire: a hard positional shake with a red-orange
        /// flash that fades back out. Runs alongside whatever state the round is in (reloading,
        /// spent), without touching it.
        /// </summary>
        public void DryFire()
        {
            // Complete (not Kill): a shake that is cut short would leave the icon off its ring slot.
            _dryShakeTween?.Complete();
            _dryShakeTween = _rect.DOShakeAnchorPos(m_DryShakeDuration, m_DryShakeStrength, m_DryShakeVibrato, 90f, false, true);

            // The tint goes through the CanvasRenderers, not Image.color: it multiplies over the
            // round's own colour (so it never fights the fire/reload colour tweens) and does not
            // rebuild the UI mesh.
            _dryTintTween?.Kill();
            ApplyDryTint(1f);
            _dryTintTween = DOTween.To(_dryTintGetter, _dryTintSetter, 0f, m_DryShakeDuration).SetEase(Ease.InQuad);
        }

        private void ApplyDryTint(float t)
        {
            _dryTint = t;
            Color c = Color.LerpUnclamped(Color.white, m_DryTint, t);
            if (_socketRenderer != null) _socketRenderer.SetColor(c);
            _fillRenderer.SetColor(c);
        }

        private void KillRoundTweens()
        {
            _scaleTween?.Kill();
            _pulseTween?.Kill();
            _fireTween?.Kill();
            _scaleTween = _pulseTween = _fireTween = null;
            _reloadPending = false;
        }

        private void OnDestroy()
        {
            KillRoundTweens();
            _dryShakeTween?.Kill();
            _dryTintTween?.Kill();
        }
    }
}
