using System.Collections.Generic;
using Loadout;
using Monetization;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>One item bundle on the Bundles tab: banner, a thumbnail per item, and a contents summary.</summary>
    public class ShopBundleCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private Image m_Banner;
        [Tooltip("Parent of the per-item thumbnails.")]
        [SerializeField] private Transform m_ItemIconRow;
        [Tooltip("Cloned for each item in the bundle.")]
        [SerializeField] private Image m_ItemIconTemplate;
        [SerializeField] private TMP_Text m_ContentsText;
        [SerializeField] private ShopPriceTag m_Price;

        private readonly List<Image> _itemIcons = new();

        public void Bind(ItemBundleSO bundle, IShopService shop)
        {
            if (m_NameText != null) m_NameText.text = bundle.DisplayName;
            if (m_ContentsText != null) m_ContentsText.text = bundle.ContentsLabel;
            if (m_Banner != null)
            {
                m_Banner.sprite = bundle.Icon;
                m_Banner.enabled = bundle.Icon != null;
            }
            BindItemIcons(bundle.Items);

            if (m_Price == null) return;

            if (shop.IsOwned(bundle))
                m_Price.ShowOwned();
            else
                m_Price.ShowMoney(shop.GetPrice(bundle.ProductId), shop.CanStartPurchase, () => shop.BuyProduct(bundle.ProductId));
        }

        private void BindItemIcons(IReadOnlyList<LoadoutItemSO> items)
        {
            if (m_ItemIconTemplate == null || m_ItemIconRow == null) return;

            m_ItemIconTemplate.gameObject.SetActive(false);
            while (_itemIcons.Count < items.Count)
                _itemIcons.Add(Instantiate(m_ItemIconTemplate, m_ItemIconRow));

            for (int i = 0; i < _itemIcons.Count; i++)
            {
                bool used = i < items.Count && items[i] != null;
                _itemIcons[i].gameObject.SetActive(used);
                if (used) _itemIcons[i].sprite = items[i].Icon;
            }
        }
    }
}
