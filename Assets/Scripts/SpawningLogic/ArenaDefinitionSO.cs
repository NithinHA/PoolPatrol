using System.Collections.Generic;
using UnityEngine;

namespace SpawningLogic
{
    [CreateAssetMenu(menuName = "PoolPatrol/Spawning/Arena Definition", fileName = "ArenaDefinition")]
    public class ArenaDefinitionSO : ScriptableObject
    {
        public string ArenaName;
        [TextArea] public string Description;
        public Sprite CardPreview;

        [Tooltip("Ordered list of levels (up to 5). Each entry is the spawn config for that level.")]
        public List<ArenaSpawnConfigSO> Levels = new();

        public int LevelCount => Levels.Count;
    }
}
