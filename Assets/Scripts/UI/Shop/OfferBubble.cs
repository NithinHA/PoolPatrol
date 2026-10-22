using System;
using DG.Tweening;
using UnityEngine;

namespace UI.Shop
{
    /// <summary>
    /// Motion for one offer bubble in the Goddess shop. Presentation only — it knows nothing about
    /// offers or gems; <see cref="AbilityShopUI"/> tells it when to arrive, pop, float off or shrink.
    ///
    /// Lives next to <see cref="AbilityOfferCard"/> on each Option_N object. The shop adds it at
    /// runtime if it is missing, so only add it by hand when you want to tune a bubble.
    ///
    /// The bubble's authored anchored position is its resting spot. Every animation is played on
    /// unscaled time, because the shop freezes the game through <c>Time.timeScale</c>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class OfferBubble : MonoBehaviour
    {
        [Header("Intro")]
        [Tooltip("Where the bubble starts, as a blend from the panel centre (0) to its resting spot (1).")]
        [Range(0f, 1f)]
        [SerializeField] private float m_IntroStartSpread = 0.25f;

        [Tooltip("Extra drop below that start point, so the bubble visibly floats up into place.")]
        [SerializeField] private float m_IntroRise = 140f;

        [SerializeField] private float m_IntroStartScale = 0.6f;
        [SerializeField] private float m_IntroDuration = 0.55f;

        [Header("Hover")]
        [Tooltip("Vertical bob distance while the bubble sits on screen.")]
        [SerializeField] private float m_HoverAmplitude = 10f;

        [Tooltip("Seconds for one full up-and-down bob.")]
        [SerializeField] private float m_HoverPeriod = 2.4f;

        [Tooltip("Scale breathing on top of the bob, e.g. 0.025 = +2.5%. Runs on a slightly different " +
                 "period so the motion never looks mechanical.")]
        [SerializeField] private float m_HoverScale = 0.025f;

        [Header("Pop (purchase)")]
        [SerializeField] private float m_PopSquashDuration = 0.07f;
        [SerializeField] private float m_PopBurstDuration = 0.14f;
        [SerializeField] private float m_PopBurstScale = 1.35f;

        [Header("Float away (refresh)")]
        [Tooltip("Margin past the top edge of the panel before the bubble counts as gone.")]
        [SerializeField] private float m_FloatAwayOvershoot = 80f;
        [SerializeField] private float m_FloatAwayDuration = 0.5f;

        [Header("Shrink (close)")]
        [SerializeField] private float m_ShrinkDuration = 0.22f;

        /// <summary>Raised the moment the pop starts — hook SFX, particles or a splash ring here.</summary>
        public event Action Popped;

        public float FloatAwayDuration => m_FloatAwayDuration;
        public float ShrinkDuration => m_ShrinkDuration;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Vector2 _home;
        private bool _initialised;

        private Sequence _sequence;
        private Tween _hoverBob;
        private Tween _hoverBreath;

        private void OnDisable() => Kill();

        private void OnDestroy() => Kill();

        /// <summary>
        /// Captures the resting spot the first time the bubble is used, i.e. while it still sits
        /// where it was authored. Lazy because the shop starts disabled, so Awake may run late.
        /// </summary>
        private void EnsureInitialised()
        {
            if (_initialised)
                return;
            _initialised = true;

            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;

            if (!TryGetComponent(out _group))
                _group = gameObject.AddComponent<CanvasGroup>();
        }

#region Animations

        /// <summary>Scales up and floats from near the panel centre to the resting spot, then hovers.</summary>
        public void PlayIntro(float delay)
        {
            EnsureInitialised();
            Kill();

            // Work out the centre offset from the resting spot so this holds for any anchor setup.
            _rect.anchoredPosition = _home;
            Vector2 toCentre = ParentCentre() - (Vector2)_rect.localPosition;
            Vector2 start = _home + toCentre * (1f - m_IntroStartSpread) + Vector2.down * m_IntroRise;

            _rect.anchoredPosition = start;
            _rect.localScale = Vector3.one * m_IntroStartScale;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            _sequence = DOTween.Sequence().SetUpdate(true)
                .AppendInterval(delay)
                .Append(_rect.DOAnchorPos(_home, m_IntroDuration).SetEase(Ease.OutCubic))
                .Join(_rect.DOScale(1f, m_IntroDuration).SetEase(Ease.OutBack))
                .Join(_group.DOFade(1f, m_IntroDuration * 0.4f).SetEase(Ease.OutQuad))
                .OnComplete(() =>
                {
                    _group.blocksRaycasts = true;
                    StartHover();
                });
        }

        /// <summary>Quick squash, then bursts outward while fading. Invokes the callback once gone.</summary>
        public void PlayPop(Action onComplete = null)
        {
            EnsureInitialised();
            Kill();
            _group.blocksRaycasts = false;

            Popped?.Invoke();

            Vector3 scale = _rect.localScale;
            _sequence = DOTween.Sequence().SetUpdate(true)
                .Append(_rect.DOScale(new Vector3(scale.x * 1.12f, scale.y * 0.88f, scale.z), m_PopSquashDuration)
                    .SetEase(Ease.OutQuad))
                .Append(_rect.DOScale(scale * m_PopBurstScale, m_PopBurstDuration).SetEase(Ease.OutQuad))
                .Join(_group.DOFade(0f, m_PopBurstDuration).SetEase(Ease.InQuad))
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>Rises past the top of the panel, accelerating like a released bubble (refresh).</summary>
        public void PlayFloatAway(Action onComplete = null)
        {
            EnsureInitialised();
            Kill();
            _group.blocksRaycasts = false;

            float distance = DistanceToClearTop();
            _sequence = DOTween.Sequence().SetUpdate(true)
                .Append(_rect.DOAnchorPosY(_rect.anchoredPosition.y + distance, m_FloatAwayDuration)
                    .SetEase(Ease.InQuad))
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>Shrinks to nothing in place (close).</summary>
        public void PlayShrink(Action onComplete = null)
        {
            EnsureInitialised();
            Kill();
            _group.blocksRaycasts = false;

            _sequence = DOTween.Sequence().SetUpdate(true)
                .Append(_rect.DOScale(0f, m_ShrinkDuration).SetEase(Ease.InBack))
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>Stops everything and puts the bubble back on its resting spot, fully visible.</summary>
        public void ResetToRest()
        {
            if (!_initialised)
                return;

            Kill();
            _rect.anchoredPosition = _home;
            _rect.localScale = Vector3.one;
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
        }

#endregion

        private void StartHover()
        {
            _hoverBob = _rect.DOAnchorPosY(_home.y + m_HoverAmplitude, m_HoverPeriod * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);

            if (m_HoverScale > 0f)
            {
                _hoverBreath = _rect.DOScale(1f + m_HoverScale, m_HoverPeriod * 0.65f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        private Vector2 ParentCentre()
            => _rect.parent is RectTransform parent ? parent.rect.center : Vector2.zero;

        /// <summary>How far up the bubble must travel for its bottom edge to leave the panel.</summary>
        private float DistanceToClearTop()
        {
            if (_rect.parent is not RectTransform parent)
                return 2000f;

            float bottomEdge = _rect.localPosition.y + _rect.rect.yMin * _rect.localScale.y;
            return Mathf.Max(0f, parent.rect.yMax - bottomEdge) + m_FloatAwayOvershoot;
        }

        private void Kill()
        {
            _sequence?.Kill();
            _hoverBob?.Kill();
            _hoverBreath?.Kill();
            _sequence = null;
            _hoverBob = null;
            _hoverBreath = null;
        }
    }
}
