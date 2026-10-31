using Pooling;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;

namespace Enemy.Death
{
    /// <summary>
    /// Gives the enemy a small chance to drop a coin pouch on death, capped per run. Sits on the
    /// gem-dropper duck next to <see cref="DropGemsOnDeath"/>; the chance and cap here are that
    /// duck's coin config. The per-run count lives on the run stats, so it resets with every run
    /// and is shared by every dropper in the arena.
    /// </summary>
    public class DropCoinPouchOnDeath : MonoBehaviour, IOnDeathEffect
    {
        [SerializeField] private PoolableItemType m_PouchType = PoolableItemType.DroppedCoinPouch;

        [Tooltip("Chance (0..1) that this enemy drops a pouch when it dies.")]
        [Range(0f, 1f)] [SerializeField] private float m_DropChance = 0.15f;

        [Tooltip("Most pouches that can drop in one run, across all droppers.")]
        [Min(0)] [SerializeField] private int m_MaxPerRun = 3;

        [Tooltip("Offset from the enemy so the pouch doesn't land under the gems.")]
        [SerializeField] private float m_DropRadius = 0.5f;

        public void Execute(EnemyDeathParameters parameters)
        {
            IRunStatsService runStats = ServiceLocator.GetRunStatsService();
            if (runStats == null || !runStats.IsRunActive || runStats.CoinPouchesDropped >= m_MaxPerRun)
                return;

            if (Random.value >= m_DropChance)
                return;

            runStats.RecordCoinPouchDrop();
            Vector2 offset = Random.insideUnitCircle * m_DropRadius;
            ObjectPoolManager.Instance.SpawnItem(m_PouchType, transform.position + (Vector3)offset, Quaternion.identity);
        }
    }
}
