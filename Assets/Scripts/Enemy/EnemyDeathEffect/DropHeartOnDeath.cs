using UnityEngine;

namespace Enemy.Death
{
    public class DropHeartOnDeath : MonoBehaviour, IOnDeathEffect
    {
        public GameObject m_HeartPrefab;

        public void Execute(EnemyDeathParameters parameters)
        {
            Instantiate(m_HeartPrefab, transform.position, Quaternion.identity);
        }
    }
}