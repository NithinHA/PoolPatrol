using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Death
{
    public class DropHeartOnDeath : MonoBehaviour, IOnDeathEffect
    {
        public GameObject heartPrefab;

        public void Execute(EnemyDeathParameters parameters)
        {
            Instantiate(heartPrefab, transform.position, Quaternion.identity);
        }
    }
}