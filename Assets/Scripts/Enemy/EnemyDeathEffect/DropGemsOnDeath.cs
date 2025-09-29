using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Death
{
    public class DropGemsOnDeath : MonoBehaviour, IOnDeathEffect
    {
        public GameObject gemPrefab;
        public int amount = 3;

        public void Execute(Dictionary<string, object> parameters)
        {
            for (int i = 0; i < amount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.5f;
                Instantiate(gemPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            }
        }
    }

}