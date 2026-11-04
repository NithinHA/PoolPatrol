using Loadout;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>One character / floatie / weapon on a Shop tab: name, picture, and how to get it.</summary>
    public class ShopItemCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private Image m_Icon;
        [SerializeField] private ShopPriceTag m_Price;

        public void Bind(LoadoutItemSO item, ILoadoutService loadout, IShopService shop)
        {
            if (m_NameText != null) m_NameText.text = item.DisplayName;
            if (m_Icon != null)
            {
                m_Icon.sprite = item.Icon;
                m_Icon.enabled = item.Icon != null;
            }

            if (m_Price == null) return;

            if (loadout.IsUnlocked(item))
            {
                m_Price.ShowOwned();
                return;
            }

            switch (item.Unlock)
            {
                case UnlockType.Coins:
                    m_Price.ShowCoins(item.CoinPrice, () => shop.BuyWithCoins(item));
                    break;

                case UnlockType.IAP:
                    m_Price.ShowMoney(shop.GetPrice(item.IapProductId), shop.CanStartPurchase, () => shop.BuyProduct(item.IapProductId));
                    break;

                case UnlockType.ArenaClear:
                    m_Price.ShowInfo(ArenaName(item.ArenaIndex));
                    break;

                default:    // BundleOnly items are only listed once owned, so this is just a safe default.
                    m_Price.ShowInfo("In a bundle");
                    break;
            }
        }

        private static string ArenaName(int arenaIndex)
        {
            IProgression progression = ServiceLocator.GetProgressionService();
            if (progression != null && arenaIndex >= 0 && arenaIndex < progression.ArenaCount)
            {
                string arenaName = progression.GetArenaDefinition(arenaIndex).ArenaName;
                if (!string.IsNullOrEmpty(arenaName)) return arenaName;
            }
            return $"Arena {arenaIndex + 1}";
        }
    }
}
