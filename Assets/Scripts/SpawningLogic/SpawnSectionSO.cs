using System.Collections.Generic;
using Enemy;
using UnityEngine;

namespace SpawningLogic
{
    /// <summary>
    /// Base data for one segment of an arena's spawn timeline. Concrete sections
    /// (<see cref="CooldownSectionSO"/>, <see cref="WaveSectionSO"/>) add their own pacing
    /// parameters; everything shared (which enemies can spawn, how many at once) lives here.
    /// </summary>
    public abstract class SpawnSectionSO : ScriptableObject
    {
        [Header("Enemy Bucket")]
        [Tooltip("Enemies allowed to spawn during this section, with relative weights.")]
        public List<WeightedEnemy> EnemyBucket = new();

        [Header("Concurrency")]
        [Min(1)] public int MaxConcurrentEnemies = 6;

        [Header("Spawn Rate")]
        [Min(0f)] public float SpawnIntervalMin = 1f;
        [Min(0f)] public float SpawnIntervalMax = 2f;

        [Header("Difficulty Biasing")]
        [Tooltip("How strongly the section's difficulty 'pressure' biases which enemy spawns.\n" +
                 "0 = ignore pressure entirely (pure designer weights, old behaviour).\n" +
                 "1 = strongly favour enemies whose relative hardness matches the current pressure.\n" +
                 "Uses each enemy's ThreatCost to rank hardness within this bucket.")]
        [Range(0f, 1f)] public float PressureBias = 0.75f;

        public float NextSpawnInterval() => Random.Range(SpawnIntervalMin, SpawnIntervalMax);

        /// <summary>
        /// Picks one enemy prefab from <see cref="EnemyBucket"/>, biasing by the section's difficulty
        /// <paramref name="pressure01"/> (0 = easiest moment, 1 = hardest). Each enemy's hardness is its
        /// ThreatCost normalized against the min/max ThreatCost in this bucket; enemies whose hardness
        /// is near the current pressure are favoured, scaled by <see cref="PressureBias"/> and the
        /// designer's base <see cref="WeightedEnemy.Weight"/>. So early in a section (low pressure) light
        /// enemies dominate; late (high pressure) tough ones do - using only the enemies already in the
        /// bucket.
        /// </summary>
        public EnemyController PickWeightedEnemy(float pressure01)
        {
            if (EnemyBucket == null || EnemyBucket.Count == 0)
                return null;

            // Find the ThreatCost range so we can normalize each enemy's hardness to 0..1.
            float minThreat = float.MaxValue, maxThreat = float.MinValue;
            foreach (var entry in EnemyBucket)
            {
                if (entry.Prefab == null) continue;
                float t = entry.Prefab.ThreatCost;
                if (t < minThreat) minThreat = t;
                if (t > maxThreat) maxThreat = t;
            }
            float threatRange = maxThreat - minThreat;

            pressure01 = Mathf.Clamp01(pressure01);

            float totalWeight = 0f;
            foreach (var entry in EnemyBucket)
            {
                if (entry.Prefab == null) continue;
                totalWeight += EffectiveWeight(entry, minThreat, threatRange, pressure01);
            }

            if (totalWeight <= 0f)
                return FirstNonNull();

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var entry in EnemyBucket)
            {
                if (entry.Prefab == null) continue;
                cumulative += EffectiveWeight(entry, minThreat, threatRange, pressure01);
                if (roll <= cumulative)
                    return entry.Prefab;
            }

            return FirstNonNull();
        }

        /// <summary>Base weight scaled by how well the enemy's hardness matches the current pressure.</summary>
        private float EffectiveWeight(WeightedEnemy entry, float minThreat, float threatRange, float pressure01)
        {
            float baseWeight = Mathf.Max(0f, entry.Weight);
            if (baseWeight <= 0f) return 0f;

            // Hardness 0..1 within this bucket (0.5 if every enemy shares the same ThreatCost).
            float hardness = threatRange > 0.0001f
                ? (entry.Prefab.ThreatCost - minThreat) / threatRange
                : 0.5f;

            // Triangular affinity: 1 when hardness == pressure, falling to 0 at the far end.
            // A floor keeps every enemy at least occasionally eligible so buckets never fully starve.
            float affinity = 1f - Mathf.Abs(hardness - pressure01);
            const float affinityFloor = 0.05f;
            affinity = Mathf.Lerp(affinityFloor, 1f, Mathf.Clamp01(affinity));

            // Blend between "ignore pressure" (base weight) and "fully pressure-driven".
            return baseWeight * Mathf.Lerp(1f, affinity, PressureBias);
        }

        private EnemyController FirstNonNull()
        {
            foreach (var entry in EnemyBucket)
                if (entry.Prefab != null)
                    return entry.Prefab;
            return null;
        }
    }
}
