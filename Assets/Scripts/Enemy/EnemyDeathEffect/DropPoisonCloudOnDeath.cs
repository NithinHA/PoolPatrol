using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Death
{
    public class DropPoisonCloudOnDeath : MonoBehaviour, IOnDeathEffect
    {
        public GameObject poisonCloudPrefab;

        public void Execute(Dictionary<string, object> parameters)
        {
            Instantiate(poisonCloudPrefab, transform.position, Quaternion.identity);
        }
    }
}