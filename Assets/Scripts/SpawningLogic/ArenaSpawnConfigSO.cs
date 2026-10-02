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

        [Tooltip("How often the Pool Goddess surfaces during this level and how long the player has " +
                 "to reach her (doc §25).")]
        public GoddessEncounters Goddess = new();

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

        /// <summary>
        /// Per-level Pool Goddess pacing (doc §4, §6, §25). Visits are placed along the arena's
        /// weighted progress rather than on a wall clock, so they land at the same points in the
        /// fight whether the player clears waves fast or slow — but the exact moment stays hidden
        /// from the player thanks to the jitter and the minimum-interval guard.
        /// </summary>
        [Serializable]
        public class GoddessEncounters
        {
            [Tooltip("How many times she surfaces during this level. 0 disables her entirely " +
                     "(short arena ~2, medium ~3, long ~4).")]
            [Min(0)] public int VisitCount = 3;

            [Tooltip("Seconds she stays reachable before retreating (doc §6).")]
            [Min(1f)] public float AvailabilityWindow = 15f;

            [Tooltip("Random shift applied to each visit's scheduled position on the progress bar, " +
                     "so repeated runs don't feel scripted. 0.05 = up to 5% of the run either way.")]
            [Range(0f, 0.25f)] public float ScheduleJitter = 0.06f;

            [Tooltip("Hard floor on seconds between the end of one visit and the start of the next " +
                     "(doc §25 recommends 90-120s for a full-length arena).")]
            [Min(0f)] public float MinSecondsBetweenVisits = 60f;

            [Tooltip("How many offers she holds out. The doc's mock shows two; three fits the " +
                     "current ShopScreen layout.")]
            [Range(1, 4)] public int OfferCount = 3;

            [Tooltip("Gem cost to discard both offers and roll new ones (doc §20). 0 hides the " +
                     "refresh button.")]
            [Min(0)] public int RefreshCost = 50;

            [Tooltip("Extra gems added to the refresh cost after each use within a single visit. " +
                     "0 keeps refresh at a flat price.")]
            [Min(0)] public int RefreshCostIncrement = 25;

            [Tooltip("Refreshes allowed per visit. 0 = unlimited (cost escalation is then the " +
                     "only brake).")]
            [Min(0)] public int MaxRefreshesPerVisit = 3;

            [Tooltip("Seconds of damage immunity granted when the shop closes, so the player can " +
                     "re-read the board before taking a hit.")]
            [Min(0f)] public float ReentryGraceSeconds = 3f;
        }
    }
}
