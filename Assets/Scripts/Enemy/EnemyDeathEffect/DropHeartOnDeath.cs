using Pooling;
using UnityEngine;

namespace Enemy.Death
{
    public class DropHeartOnDeath : MonoBehaviour, IOnDeathEffect
    {
        public PoolableItemType m_HeartType = PoolableItemType.DroppedHeart;

        public void Execute(EnemyDeathParameters parameters)
        {
            ObjectPoolManager.Instance.SpawnItem(
                m_HeartType, transform.position, Quaternion.identity);
        }
    }
}
