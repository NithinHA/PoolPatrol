using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Death
{
    public class DropGemsOnDeath : MonoBehaviour, IOnDeathEffect
    {
        public GameObject m_GemPrefab;
        public int m_Amount = 3;
        public float m_DropRadius = 1f;

        public void Execute(EnemyDeathParameters parameters)
        {
            for (int i = 0; i < m_Amount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * m_DropRadius;
                Instantiate(m_GemPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            }
        }
    }

}