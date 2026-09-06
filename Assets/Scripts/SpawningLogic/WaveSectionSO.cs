using UnityEngine;

namespace SpawningLogic
{
    /// <summary>
    /// A deliberate "big wave" beat: spawns a fixed <see cref="EnemyCount"/> enemies from
    /// <see cref="SpawnSectionSO.EnemyBucket"/> in bursts, then gates the arena's progress until
    /// every enemy currently in the arena is dead (PvZ-style flag wave). The arena only advances to
    /// the next section once the last enemy from the wave falls.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Spawning/Wave Section", fileName = "WaveSection")]
    public class WaveSectionSO : SpawnSectionSO
    {
        [Header("Wave")]
        [Tooltip("Total number of enemies spawned over the course of this wave.")]
        [Min(1)] public int EnemyCount = 10;

        [Tooltip("How many enemies are spawned per burst, respecting MaxConcurrentEnemies.")]
        [Min(1)] public int SpawnBurstSize = 1;

        [Tooltip("Marks this as a boss/finale wave. Purely informational for now (e.g. UI, audio cues).")]
        public bool IsBoss;
    }
}
