using System.Collections.Generic;
using System.IO;
using SpawningLogic;
using UnityEditor;
using UnityEngine;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// Builds a dummy <see cref="ArenaSpawnConfigSO"/> for the default arena, wired up with the six
    /// RubberDuck prefabs under Assets/Prefabs/Enemies. Meant as a working example/starting point to
    /// tune, not final balance.
    /// </summary>
    public static class CreateDefaultArenaConfig
    {
        private const string SectionsFolder = "Assets/ScriptableObjects/Arenas/Default/Sections";
        private const string ConfigPath      = "Assets/ScriptableObjects/Arenas/Default/DefaultArenaSpawnConfig.asset";
        private const string EnemiesFolder   = "Assets/Prefabs/Enemies";

        [MenuItem("PoolPatrol/Create Default Arena Spawn Config")]
        public static void Create()
        {
            if (!Directory.Exists(SectionsFolder))
                Directory.CreateDirectory(SectionsFolder);

            var idle         = LoadEnemy("RubberDuck_idle");
            var randomMove   = LoadEnemy("RubberDuck_randomMove");
            var chase        = LoadEnemy("RubberDuck_chase");
            var shooter      = LoadEnemy("RubberDuck_shooter");
            var gemDropper   = LoadEnemy("RubberDuck_gemDropper");
            var heartDropper = LoadEnemy("RubberDuck_heartDropper");

            var sections = new List<SpawnSectionSO>
            {
                // ── Warmup ──────────────────────────────────────────────────
                CreateCooldown("Cooldown_01_Warmup", new CooldownSpec
                {
                    Duration = 45f,
                    ProgressWeight = 10f, // short, and only worth a small sliver of the bar
                    MaxConcurrentEnemies = 4,
                    SpawnIntervalMin = 2f,
                    SpawnIntervalMax = 3.5f,
                    MinThreatBudget = 2f,  // very light start
                    MaxThreatBudget = 6f,
                    Bucket = new()
                    {
                        Weighted(idle, 3f),
                        Weighted(randomMove, 2f),
                    },
                }),

                // ── First wave ──────────────────────────────────────────────
                CreateWave("Wave_01_FirstWave", new WaveSpec
                {
                    EnemyCount = 8,
                    SpawnBurstSize = 2,
                    MaxConcurrentEnemies = 6,
                    SpawnIntervalMin = 0.4f,
                    SpawnIntervalMax = 0.8f,
                    Bucket = new()
                    {
                        Weighted(idle, 2f),
                        Weighted(randomMove, 2f),
                        Weighted(chase, 1f),
                    },
                }),

                // ── Building up ─────────────────────────────────────────────
                CreateCooldown("Cooldown_02_BuildingUp", new CooldownSpec
                {
                    Duration = 60f,
                    ProgressWeight = 25f, // longer, and worth a bigger chunk of the bar
                    MaxConcurrentEnemies = 6,
                    SpawnIntervalMin = 1.5f,
                    SpawnIntervalMax = 2.5f,
                    MinThreatBudget = 5f,
                    MaxThreatBudget = 14f,
                    Bucket = new()
                    {
                        Weighted(randomMove, 3f),
                        Weighted(chase, 2f),
                    },
                }),

                // ── Chaser swarm ─────────────────────────────────────────────
                CreateWave("Wave_02_ChaserSwarm", new WaveSpec
                {
                    EnemyCount = 12,
                    SpawnBurstSize = 3,
                    MaxConcurrentEnemies = 8,
                    SpawnIntervalMin = 0.3f,
                    SpawnIntervalMax = 0.6f,
                    Bucket = new()
                    {
                        Weighted(chase, 3f),
                        Weighted(shooter, 1f),
                        Weighted(randomMove, 1f),
                    },
                }),

                // ── Regroup ──────────────────────────────────────────────────
                CreateCooldown("Cooldown_03_Regroup", new CooldownSpec
                {
                    Duration = 60f,
                    ProgressWeight = 25f, // same duration as BuildingUp, same bar share
                    MaxConcurrentEnemies = 7,
                    SpawnIntervalMin = 1.2f,
                    SpawnIntervalMax = 2f,
                    MinThreatBudget = 8f,
                    MaxThreatBudget = 20f,
                    Bucket = new()
                    {
                        Weighted(chase, 2f),
                        Weighted(shooter, 2f),
                    },
                }),

                // ── Shooter gauntlet ─────────────────────────────────────────
                CreateWave("Wave_03_ShooterGauntlet", new WaveSpec
                {
                    EnemyCount = 14,
                    SpawnBurstSize = 2,
                    MaxConcurrentEnemies = 8,
                    SpawnIntervalMin = 0.3f,
                    SpawnIntervalMax = 0.6f,
                    Bucket = new()
                    {
                        Weighted(shooter, 3f),
                        Weighted(chase, 2f),
                    },
                }),

                // ── Final calm ───────────────────────────────────────────────
                CreateCooldown("Cooldown_04_FinalCalm", new CooldownSpec
                {
                    Duration = 45f,
                    ProgressWeight = 40f, // shorter than BuildingUp/Regroup but the biggest bar share -
                                          // deliberately non-proportional, to demonstrate ProgressWeight
                                          // is independent of Duration.
                    MaxConcurrentEnemies = 8,
                    SpawnIntervalMin = 1f,
                    SpawnIntervalMax = 1.8f,
                    MinThreatBudget = 10f,
                    MaxThreatBudget = 24f,
                    Bucket = new()
                    {
                        Weighted(chase, 2f),
                        Weighted(shooter, 2f),
                    },
                }),

                // ── Last stand (finale wave) ─────────────────────────────────
                CreateWave("Wave_04_LastStand", new WaveSpec
                {
                    EnemyCount = 18,
                    SpawnBurstSize = 3,
                    MaxConcurrentEnemies = 10,
                    SpawnIntervalMin = 0.25f,
                    SpawnIntervalMax = 0.5f,
                    IsBoss = true,
                    Bucket = new()
                    {
                        Weighted(shooter, 3f),
                        Weighted(chase, 3f),
                        Weighted(randomMove, 1f),
                    },
                }),
            };

            var config = AssetDatabase.LoadAssetAtPath<ArenaSpawnConfigSO>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ArenaSpawnConfigSO>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.Sections = sections;

            // Reward economy - decoupled from the combat buckets above. Gems are budget-driven
            // (predictable affordability); hearts are player-aware (spawn only when hurt).
            config.Rewards = new ArenaSpawnConfigSO.RewardBudget
            {
                GemDropperPrefab      = gemDropper,
                TargetGemDropperCount = 12,
                GemMinInterval        = 2f,
                HeartDropperPrefab    = heartDropper,
                MaxHeartDroppers      = 4,
                BarelyHurtDelay       = 60f,
                CriticalDelay         = 10f,
                PostSpawnCooldown     = 45f,
            };

            EditorUtility.SetDirty(config);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PoolPatrol] Created {sections.Count} default arena sections and populated {ConfigPath}.");
        }

        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Assigns sensible default ThreatCost values to the RubberDuck prefabs so the difficulty
        /// director has meaningful hardness ratings out of the box. Tune per prefab afterwards.
        /// </summary>
        [MenuItem("PoolPatrol/Assign Default Enemy Threat Costs")]
        public static void AssignDefaultThreatCosts()
        {
            SetThreat("RubberDuck_idle", 1f);
            SetThreat("RubberDuck_randomMove", 2f);
            SetThreat("RubberDuck_gemDropper", 2f);
            SetThreat("RubberDuck_heartDropper", 2f);
            SetThreat("RubberDuck_chase", 4f);
            SetThreat("RubberDuck_shooter", 5f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PoolPatrol] Assigned default ThreatCost values to enemy prefabs.");
        }

        private static void SetThreat(string prefabName, float threat)
        {
            var controller = LoadEnemy(prefabName);
            if (controller == null) return;

            controller.ThreatCost = threat;
            EditorUtility.SetDirty(controller);
            PrefabUtility.SavePrefabAsset(controller.gameObject);
        }

        private struct CooldownSpec
        {
            public float Duration;
            public float ProgressWeight;
            public int MaxConcurrentEnemies;
            public float SpawnIntervalMin;
            public float SpawnIntervalMax;
            public float MinThreatBudget;
            public float MaxThreatBudget;
            public List<WeightedEnemy> Bucket;
        }

        private struct WaveSpec
        {
            public int EnemyCount;
            public int SpawnBurstSize;
            public int MaxConcurrentEnemies;
            public float SpawnIntervalMin;
            public float SpawnIntervalMax;
            public bool IsBoss;
            public List<WeightedEnemy> Bucket;
        }

        private static CooldownSectionSO CreateCooldown(string assetName, CooldownSpec spec)
        {
            var section = LoadOrCreate<CooldownSectionSO>(assetName);
            section.Duration = spec.Duration;
            section.ProgressWeight = spec.ProgressWeight > 0f ? spec.ProgressWeight : 10f;
            section.MaxConcurrentEnemies = spec.MaxConcurrentEnemies;
            section.SpawnIntervalMin = spec.SpawnIntervalMin;
            section.SpawnIntervalMax = spec.SpawnIntervalMax;
            section.MinThreatBudget = spec.MinThreatBudget > 0f ? spec.MinThreatBudget : 3f;
            section.MaxThreatBudget = spec.MaxThreatBudget > 0f ? spec.MaxThreatBudget : 10f;
            section.DifficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            section.EnemyBucket = spec.Bucket;
            EditorUtility.SetDirty(section);
            return section;
        }

        private static WaveSectionSO CreateWave(string assetName, WaveSpec spec)
        {
            var section = LoadOrCreate<WaveSectionSO>(assetName);
            section.EnemyCount = spec.EnemyCount;
            section.SpawnBurstSize = spec.SpawnBurstSize;
            section.MaxConcurrentEnemies = spec.MaxConcurrentEnemies;
            section.SpawnIntervalMin = spec.SpawnIntervalMin;
            section.SpawnIntervalMax = spec.SpawnIntervalMax;
            section.IsBoss = spec.IsBoss;
            section.EnemyBucket = spec.Bucket;
            EditorUtility.SetDirty(section);
            return section;
        }

        private static T LoadOrCreate<T>(string assetName) where T : ScriptableObject
        {
            string path = $"{SectionsFolder}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static WeightedEnemy Weighted(Enemy.EnemyController prefab, float weight) =>
            new() { Prefab = prefab, Weight = weight };

        private static Enemy.EnemyController LoadEnemy(string prefabName)
        {
            string path = $"{EnemiesFolder}/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[PoolPatrol] Could not find enemy prefab at '{path}'.");
                return null;
            }

            var controller = prefab.GetComponent<Enemy.EnemyController>();
            if (controller == null)
                Debug.LogError($"[PoolPatrol] Prefab '{path}' has no EnemyController component.");

            return controller;
        }
    }
}
