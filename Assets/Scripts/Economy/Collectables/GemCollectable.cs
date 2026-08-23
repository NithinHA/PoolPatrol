using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;

namespace Economy
{
    /// <summary>
    /// Gem pickup. On grant, adds its value to the run-scoped Gems balance via the economy service,
    /// then flies to the gem wallet HUD. Attach to gem prefabs dropped by DropGemsOnDeath.
    /// </summary>
    public class GemCollectable : CollectableBase
    {
        [Header("Gem")]
        [SerializeField] private int m_Value = 1;

        protected override CollectionTargetId TargetId => CollectionTargetId.GemWallet;

        protected override void Grant()
        {
            ServiceLocator.GetEconomyService()?.Grant(CurrencyType.Gems, m_Value);
        }
    }
}
