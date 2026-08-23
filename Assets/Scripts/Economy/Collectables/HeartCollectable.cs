using Player;
using UnityEngine;

namespace Economy
{
    /// <summary>
    /// Heart pickup. On grant, heals the local player, then flies to the lives panel HUD.
    /// Attach to heart prefabs dropped by DropHeartOnDeath.
    /// </summary>
    public class HeartCollectable : CollectableBase
    {
        [Header("Heart")]
        [SerializeField] private int m_HealAmount = 1;

        protected override CollectionTargetId TargetId => CollectionTargetId.Health;

        protected override void Grant()
        {
            PlayerController local = PlayerController.Local;
            if (local != null && local.PlayerHealth != null)
                local.PlayerHealth.Heal(m_HealAmount);
        }
    }
}
