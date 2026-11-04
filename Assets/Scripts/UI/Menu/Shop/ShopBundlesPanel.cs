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
    /// The arrows page by one viewport and hide when there is nothing further that way.
    /// </summary>
    public class ShopBundlesPanel : ShopPanel
    {
        [Header("Coin packs")]
        [SerializeField] private ShopCoinPackCard m_CoinPackPrefab;
        [SerializeField] private Transform m_CoinPackRow;

        [Header("Bundles")]
        [SerializeField] private ShopBundleCard m_BundlePrefab;
        [SerializeField] private Transform m_BundleRow;

        [Header("Paging")]
        [SerializeField] private ScrollRect m_ScrollRect;
        [SerializeField] private Button m_LeftButton;
        [SerializeField] private Button m_RightButton;
        [SerializeField] private float m_PageDuration = 0.3f;

        private readonly List<ShopCoinPackCard> _coinPacks = new();
        private readonly List<ShopBundleCard> _bundles = new();
        private Tween _pageTween;

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

            if (m_ScrollRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_ScrollRect.content);
            RefreshArrows();
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

            float pageStep = m_ScrollRect.viewport.rect.width / overflow;
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

        /// <summary>How much wider the content is than the viewport.</summary>
        private float Overflow => m_ScrollRect != null && m_ScrollRect.viewport != null
            ? m_ScrollRect.content.rect.width - m_ScrollRect.viewport.rect.width
            : 0f;
    }
}
