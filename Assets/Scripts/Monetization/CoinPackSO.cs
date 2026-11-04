using UnityEngine;

namespace Monetization
{
    /// <summary>A consumable IAP that grants a fixed number of Coins each time it is bought.</summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Shop/Coin Pack", fileName = "CoinPack")]
    public class CoinPackSO : ShopOfferSO
    {
        [Min(1)] public int Coins = 500;

        public override bool IsConsumable => true;
    }
}
