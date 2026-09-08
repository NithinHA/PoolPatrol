using System;
using System.Collections.Generic;
using Enemy;
using UnityEngine;

namespace SpawningLogic
{
    /// <summary>
    /// The full spawn timeline for one level within an arena: an ordered list of cooldown/wave
    /// sections, plus a reward economy budget. One asset per level, referenced by
    /// <see cref="ArenaDefinitionSO"/> and assigned at runtime to <see cref="ArenaDirector"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Spawning/Arena Spawn Config", fileName = "ArenaSpawnConfig")]
    public class ArenaSpawnConfigSO : ScriptableObject
    {
        [Tooltip("Sections run in order. Mix CooldownSectionSO and WaveSectionSO assets to shape the run.")]
        public List<SpawnSectionSO> Sections = new();

        [Tooltip("Reward-enemy economy for this arena, spawned separately from the combat sections so " +
                 "affordability and healing stay predictable regardless of which combat enemies roll.")]
        public RewardBudget Rewards = new();

        /// <summary>
        /// Controls the reward-dropper economy for an arena, decoupled from the combat difficulty
        /// bucket. Gems are budget-driven (a fixed target count distributed across the run, so players
        /// can reliably afford a predictable number of abilities); hearts are player-aware (only spawn
        /// when the player is below max health, capped and interval-gated, so they never inflate lives
        /// nor starve a hurt player).
        /// </summary>
        [Serializable]
        public class RewardBudget
        {
            [Header("Gems (budget-driven)")]
            [Tooltip("Gem-dropper enemy prefab. Leave null to disable gem drops for this arena.")]
            public EnemyController GemDropperPrefab;

            [Tooltip("Total gem droppers spawned across the whole arena, distributed over the run's " +
                     "progress. This is the main lever for how many abilities a run can afford.")]
            [Min(0)] public int TargetGemDropperCount = 12;

            [Tooltip("Minimum seconds between gem-dropper spawns, so they never clump.")]
            [Min(0f)] public float GemMinInterval = 2f;

            [Header("Hearts (hurt-duration gated)")]
            [Tooltip("Heart-dropper enemy prefab. Leave null to disable heart drops for this arena.")]
            public EnemyController HeartDropperPrefab;

            [Tooltip("Hard cap on heart droppers spawned across the whole arena.")]
            [Min(0)] public int MaxHeartDroppers = 4;

            [Tooltip("Seconds the player must stay hurt before a heart spawns when only 1 life is " +
                     "missing (least urgent). The actual delay lerps toward CriticalDelay as more " +
                     "lives are lost.")]
            [Min(1f)] public float BarelyHurtDelay = 60f;

            [Tooltip("Seconds the player must stay hurt before a heart spawns when on their last " +
                     "life (most urgent).")]
            [Min(1f)] public float CriticalDelay = 10f;

            [Tooltip("After a heart dropper spawns, the hurt timer freezes for this many seconds " +
                     "before it can start accumulating again. Gives the player struggling space " +
                     "instead of spawning multiple hearts back-to-back.")]
            [Min(0f)] public float PostSpawnCooldown = 45f;
        }
    }
}
