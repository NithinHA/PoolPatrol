using System.Collections.Generic;
using DG.Tweening;
using Monetization;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// The Bundles tab: a row of coin packs above a row of item bundles, scrolling sideways together.
    /// Cards keep a fixed design size (so their text and images never overflow) and the whole content
    /// is scaled uniformly so exactly <see cref="m_VisibleCards"/> cards fill the viewport width.
    /// The arrows page by that many cards and hide when there is nothing further that way.
    /// </summary>
    public class ShopBundlesPanel : ShopPanel
    {
        [Header("Coin packs")]
        [SerializeField] private ShopCoinPackCard m_CoinPackPrefab;
        [SerializeField] private Transform m_CoinPackRow;

        [Header("Bundles")]
        [SerializeField] private ShopBundleCard m_BundlePrefab;
        [SerializeField] private Transform m_BundleRow;

        [Header("Layout")]
        [Tooltip("Cards visible across the viewport at once.")]
        [Min(1)] [SerializeField] private int m_VisibleCards = 4;
        [Tooltip("Design size of every card, in canvas units. Height is what keeps the card ratio constant.")]
        [SerializeField] private Vector2 m_CoinPackCardSize = new(420f, 300f);
        [SerializeField] private Vector2 m_BundleCardSize = new(420f, 330f);

        [Header("Paging")]
        [SerializeField] private ScrollRect m_ScrollRect;
        [SerializeField] private Button m_LeftButton;
        [SerializeField] private Button m_RightButton;
        [SerializeField] private float m_PageDuration = 0.3f;

        private readonly List<ShopCoinPackCard> _coinPacks = new();
        private readonly List<ShopBundleCard> _bundles = new();
        private Tween _pageTween;
        private float _scale = 1f;

        private void OnEnable() => FitToViewport();

        private void Awake()
        {
            if (m_LeftButton != null) m_LeftButton.onClick.AddListener(() => Page(-1));
            if (m_RightButton != null) m_RightButton.onClick.AddListener(() => Page(+1));
            if (m_ScrollRect != null) m_ScrollRect.onValueChanged.AddListener(_ => RefreshArrows());
        }

        private void OnDestroy() => _pageTween?.Kill();

        public override void Refresh()
        {
            IShopService shop = ServiceLocator.GetShopService();
            if (shop == null) return;

            for (int i = 0; i < shop.CoinPacks.Count; i++)
            {
                if (i == _coinPacks.Count)
                    _coinPacks.Add(Instantiate(m_CoinPackPrefab, m_CoinPackRow));
                _coinPacks[i].Bind(shop.CoinPacks[i], shop);
            }

            for (int i = 0; i < shop.Bundles.Count; i++)
            {
                if (i == _bundles.Count)
                    _bundles.Add(Instantiate(m_BundlePrefab, m_BundleRow));
                _bundles[i].Bind(shop.Bundles[i], shop);
            }

            FitToViewport();
        }

        public override void ResetView()
        {
            _pageTween?.Kill();
            if (m_ScrollRect != null) m_ScrollRect.horizontalNormalizedPosition = 0f;
            RefreshArrows();
        }

        private void Page(int direction)
        {
            float overflow = Overflow;
            if (overflow <= 0f) return;

            float pageStep = m_VisibleCards * CardStride * _scale / overflow;
            float target = Mathf.Clamp01(m_ScrollRect.horizontalNormalizedPosition + direction * pageStep);

            _pageTween?.Kill();
            _pageTween = DOTween.To(() => m_ScrollRect.horizontalNormalizedPosition,
                    x => m_ScrollRect.horizontalNormalizedPosition = x, target, m_PageDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        private void RefreshArrows()
        {
            float position = m_ScrollRect != null ? m_ScrollRect.horizontalNormalizedPosition : 0f;
            bool scrollable = Overflow > 1f;

            if (m_LeftButton != null) m_LeftButton.gameObject.SetActive(scrollable && position > 0.001f);
            if (m_RightButton != null) m_RightButton.gameObject.SetActive(scrollable && position < 0.999f);
        }

        /// <summary>Distance between the left edges of two neighbouring cards, in design units.</summary>
        private float CardStride => m_BundleCardSize.x + RowSpacing;

        private float RowSpacing => m_BundleRow != null && m_BundleRow.TryGetComponent(out HorizontalLayoutGroup row)
            ? row.spacing
            : 0f;

        /// <summary>
        /// Gives every card its fixed design size, then scales the content uniformly so
        /// <see cref="m_VisibleCards"/> bundle cards fit the viewport in BOTH directions, so the bottom
        /// row never clips. The scroll rect itself is never resized: on a wide screen the height is the
        /// limit and a sliver of the next card shows at the right edge. An aspect-fitted shop container
        /// removes that.
        /// </summary>
        private void FitToViewport()
        {
            if (m_ScrollRect == null || m_ScrollRect.viewport == null) return;

            foreach (ShopCoinPackCard card in _coinPacks) SetCardSize(card.gameObject, m_CoinPackCardSize);
            foreach (ShopBundleCard card in _bundles) SetCardSize(card.gameObject, m_BundleCardSize);

            RectTransform content = m_ScrollRect.content;
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.localScale = Vector3.one;
            if (!content.TryGetComponent(out ContentSizeFitter fitter)) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Rect viewport = m_ScrollRect.viewport.rect;
            float designWidth = m_VisibleCards * m_BundleCardSize.x + (m_VisibleCards - 1) * RowSpacing;
            float designHeight = content.rect.height;
            if (designWidth <= 0f || designHeight <= 0f || viewport.width <= 0f || viewport.height <= 0f) return;

            _scale = Mathf.Min(viewport.width / designWidth, viewport.height / designHeight);
            content.localScale = new Vector3(_scale, _scale, 1f);

            RefreshArrows();
        }

        private static void SetCardSize(GameObject card, Vector2 size)
        {
            if (!card.TryGetComponent(out LayoutElement layout)) layout = card.AddComponent<LayoutElement>();
            layout.preferredWidth = layout.minWidth = size.x;
            layout.preferredHeight = layout.minHeight = size.y;
        }

        /// <summary>How much wider the (scaled) content is than the viewport.</summary>
        private float Overflow => m_ScrollRect != null && m_ScrollRect.viewport != null
            ? m_ScrollRect.content.rect.width * _scale - m_ScrollRect.viewport.rect.width
            : 0f;
    }
}
