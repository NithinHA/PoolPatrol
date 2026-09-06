using System;
using Enemy;
using UnityEngine;

namespace SpawningLogic
{
    /// <summary>
    /// A single entry in a section's enemy bucket: which prefab can spawn and how likely it is
    /// relative to the other entries in the same bucket.
    /// </summary>
    [Serializable]
    public struct WeightedEnemy
    {
        public EnemyController Prefab;
        [Min(0f)] public float Weight;
    }
}
