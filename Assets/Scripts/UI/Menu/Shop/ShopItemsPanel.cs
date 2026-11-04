using System.Collections.Generic;
using Loadout;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    public enum ShopCategory { Characters, Floaties, Weapons }

    /// <summary>
    /// A Characters / Floaties / Weapons tab: a grid of <see cref="m_Columns"/> columns whose cells fill the
    /// viewport width and keep a fixed height/width ratio, so cards never stretch. Extra rows scroll
    /// vertically. Reads the same loadout
    /// catalog as the loadout screen. Bundle-only items stay hidden until owned, since they aren't sold here.
    /// </summary>
    public class ShopItemsPanel : ShopPanel
    {
        [SerializeField] private ShopCategory m_Category;
        [SerializeField] private ShopItemCard m_CardPrefab;
        [SerializeField] private GridLayoutGroup m_Grid;
        [SerializeField] private ScrollRect m_ScrollRect;
        [Min(1)] [SerializeField] private int m_Columns = 5;

        [Tooltip("Card height divided by width. Cell width follows the viewport; height follows this ratio.")]
        [Min(0.1f)] [SerializeField] private float m_CardAspect = 1.3f;

        private readonly List<ShopItemCard> _cards = new();

        public override void Refresh()
        {
            ILoadoutService loadout = ServiceLocator.GetLoadoutService();
            IShopService shop = ServiceLocator.GetShopService();
            if (loadout == null || shop == null) return;

            FitGrid();

            int shown = 0;
            foreach (LoadoutItemSO item in Items(loadout))
            {
                if (item == null || (item.Unlock == UnlockType.BundleOnly && !loadout.IsUnlocked(item)))
                    continue;

                if (shown == _cards.Count)
                    _cards.Add(Instantiate(m_CardPrefab, m_Grid.transform));

                _cards[shown].gameObject.SetActive(true);
                _cards[shown].Bind(item, loadout, shop);
                shown++;
            }

            for (int i = shown; i < _cards.Count; i++)
                _cards[i].gameObject.SetActive(false);
        }

        public override void ResetView()
        {
            if (m_ScrollRect != null) m_ScrollRect.verticalNormalizedPosition = 1f;
        }

        private IReadOnlyList<LoadoutItemSO> Items(ILoadoutService loadout) => m_Category switch
        {
            ShopCategory.Characters => loadout.Characters,
            ShopCategory.Floaties => loadout.Floaties,
            _ => loadout.Weapons,
        };

        private void FitGrid()
        {
            if (m_ScrollRect == null || m_ScrollRect.viewport == null) return;

            Rect viewport = m_ScrollRect.viewport.rect;
            RectOffset padding = m_Grid.padding;
            Vector2 spacing = m_Grid.spacing;

            float width = (viewport.width - padding.horizontal - spacing.x * (m_Columns - 1)) / m_Columns;
            width = Mathf.Max(1f, width);
            m_Grid.cellSize = new Vector2(width, width * m_CardAspect);
        }
    }
}
