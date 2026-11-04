using Monetization;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>One coin pack on the Bundles tab. Always purchasable, since coin packs are consumable.</summary>
    public class ShopCoinPackCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private Image m_Icon;
        [SerializeField] private TMP_Text m_CoinsText;
        [SerializeField] private ShopPriceTag m_Price;

        public void Bind(CoinPackSO pack, IShopService shop)
        {
            if (m_NameText != null) m_NameText.text = pack.DisplayName;
            if (m_CoinsText != null) m_CoinsText.text = pack.Coins.ToString("N0");
            if (m_Icon != null)
            {
                m_Icon.sprite = pack.Icon;
                m_Icon.enabled = pack.Icon != null;
            }

            if (m_Price != null)
                m_Price.ShowMoney(shop.GetPrice(pack.ProductId), shop.CanStartPurchase, () => shop.BuyProduct(pack.ProductId));
        }
    }
}
