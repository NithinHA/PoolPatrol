using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;

namespace Economy
{
    /// <summary>
    /// Coin pouch pickup. Unlike the run-end payout, a pouch is credited the moment it is picked up:
    /// the coins go straight into the persistent Coins balance (which slides the coin wallet HUD in)
    /// and are tallied on the run stats so the summary can show them. Dropped by
    /// <c>DropCoinPouchOnDeath</c> on gem-dropper ducks.
    /// </summary>
    public class CoinCollectable : CollectableBase
    {
        [Header("Coin pouch")]
        [Tooltip("Coins this pouch can hold; one is picked at random each time it drops.")]
        [SerializeField] private int[] m_PossibleValues = { 3, 5 };

        private int _value;

        protected override CollectionTargetId TargetId => CollectionTargetId.CoinWallet;

        protected override void OnEnable()
        {
            base.OnEnable();
            _value = m_PossibleValues is { Length: > 0 }
                ? m_PossibleValues[Random.Range(0, m_PossibleValues.Length)]
                : 0;
        }

        protected override void Grant()
        {
            ServiceLocator.GetEconomyService()?.Grant(CurrencyType.Coins, _value);
            ServiceLocator.GetRunStatsService()?.AddCollectedCoins(_value);
        }
    }
}
