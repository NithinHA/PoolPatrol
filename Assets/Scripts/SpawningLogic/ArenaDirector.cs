using System;
using System.Collections;
using System.Collections.Generic;
using Enemy;
using Player;
using Pooling;
using UI.HUD;
using UnityEngine;

namespace SpawningLogic
{
    /// <summary>
    /// Drives one arena's spawn timeline: walks an <see cref="ArenaSpawnConfigSO"/>'s sections in
    /// order, spawning enemies from each section's bucket, and reports progress to an
    /// <see cref="ArenaProgressBar"/>.
    ///
    /// The bar's total length is the sum of every <see cref="CooldownSectionSO.ProgressWeight"/> in
    /// the config - not raw duration, so a long cooldown can consume a small sliver of the bar (or a
    /// short one a large chunk). Wave sections don't occupy any bar width themselves. Instead each
    /// wave sits as a zero-width checkpoint marker at the position it falls on that weighted
    /// timeline: the bar moves linearly through cooldowns (paced by real elapsed time within each
    /// section) and simply holds still (no tween) while a wave is in progress, with the wave's
    /// marker pulsating to show it's the active checkpoint. Once every enemy in the arena is dead,
    /// the marker flips to a checkmark and the bar resumes from the same position into the next
    /// cooldown - no jump needed, since the wave never moved it.
    /// Replaces the old timer-only <c>Spawner</c>.
    /// </summary>
    public class ArenaDirector : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private ArenaSpawnConfigSO m_Config;

        [Header("UI")]
        [SerializeField] private ArenaProgressBar m_ProgressBar;

        [Header("Spawn Behaviour")]
        [SerializeField] private float m_SpawnEffectDelay = 1f;
        [SerializeField] private float m_EnemySpacing = 1.5f;

        /// <summary>Raised once every section has completed and the arena is cleared.</summary>
        public event Action OnArenaComplete;

        private readonly List<EnemyController> _activeEnemies = new();
        private float _totalProgressWeight;

        // Live state read by the reward director (which runs as its own coroutine alongside the
        // section loop): how far the bar has filled (0..1), whether a wave is currently gating, and
        // whether the arena is still running at all.
        private float _arenaProgress;
        private bool _inWave;
        private bool _arenaRunning;
        private bool _began;

        private void OnEnable() => EnemyController.OnAnyEnemyDied += HandleEnemyDied;
        private void OnDisable() => EnemyController.OnAnyEnemyDied -= HandleEnemyDied;

        private void Start()
        {
            // Config may already be assigned in the inspector (standalone scene testing), or set
            // externally via Configure() before this Start() runs — either way, begin exactly once.
            if (m_Config != null)
                Begin();
        }

        /// <summary>
        /// Assigns the level's spawn config and starts the arena. Called by <see cref="LevelManager"/>
        /// once it knows which level the player selected, before this component's own Start() runs.
        /// </summary>
        public void Configure(ArenaSpawnConfigSO config)
        {
            m_Config = config;
            Begin();
        }

        private void Begin()
        {
            if (_began) return;

            if (m_Config == null || m_Config.Sections == null || m_Config.Sections.Count == 0)
            {
                Debug.LogError("[ArenaDirector] No arena config assigned, or it has no sections.");
                return;
            }

            _began = true;

            _totalProgressWeight = 0f;
            foreach (var section in m_Config.Sections)
                if (section is CooldownSectionSO cooldown)
                    _totalProgressWeight += Mathf.Max(0.01f, cooldown.ProgressWeight);
            _totalProgressWeight = Mathf.Max(_totalProgressWeight, 0.01f); // guard div-by-zero

            BuildProgressMarkers();

            _arenaRunning = true;
            StartCoroutine(RunArena());
            StartCoroutine(RunRewardDirector());
        }

        /// <summary>Places one pending marker per wave section at its cumulative-progress-weight position.</summary>
        private void BuildProgressMarkers()
        {
            if (m_ProgressBar == null)
                return;

            var positions = new List<float>();
            float weightSoFar = 0f;
            foreach (var section in m_Config.Sections)
            {
                if (section is CooldownSectionSO cooldown)
                    weightSoFar += Mathf.Max(0.01f, cooldown.ProgressWeight);
                else if (section is WaveSectionSO)
                    positions.Add(weightSoFar / _totalProgressWeight);
            }

            m_ProgressBar.BuildMarkers(positions);
            m_ProgressBar.SnapTo(0f);
        }

        private IEnumerator RunArena()
        {
            float weightSoFar = 0f;
            int nextMarkerIndex = 0;

            foreach (var section in m_Config.Sections)
            {
                switch (section)
                {
                    case CooldownSectionSO cooldown:
                    {
                        float startProgress = weightSoFar / _totalProgressWeight;
                        weightSoFar += Mathf.Max(0.01f, cooldown.ProgressWeight);
                        float endProgress = weightSoFar / _totalProgressWeight;
                        yield return RunCooldownSection(cooldown, startProgress, endProgress);
                        break;
                    }
                    case WaveSectionSO wave:
                    {
                        int markerIndex = nextMarkerIndex++;
                        yield return RunWaveSection(wave, markerIndex);
                        break;
                    }
                    default:
                        Debug.LogWarning($"[ArenaDirector] Unknown section type '{section.GetType().Name}', skipping.");
                        break;
                }
            }

            _arenaProgress = 1f;
            _arenaRunning = false;
            m_ProgressBar?.SetProgress(1f);
            OnArenaComplete?.Invoke();
        }

        private IEnumerator RunCooldownSection(CooldownSectionSO section, float startProgress, float endProgress)
        {
            float elapsed = 0f;
            float spawnTimer = section.NextSpawnInterval();

            while (elapsed < section.Duration)
            {
                yield return null;

                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / section.Duration);
                _arenaProgress = Mathf.Lerp(startProgress, endProgress, normalizedTime);
                m_ProgressBar?.SnapTo(_arenaProgress);

                // Feed-forward: difficulty pressure ramps along the section's curve.
                float pressure = section.PressureAt(normalizedTime);

                // Feedback: hold spawns while the arena is already at/over its threat target for this
                // pressure, or at the hard concurrency cap. As the player kills enemies, current threat
                // drops back under target and spawning resumes - self-regulating the crowd.
                float currentThreat = CurrentThreat();
                bool overBudget = currentThreat >= section.ThreatBudgetAt(pressure);
                if (overBudget || _activeEnemies.Count >= section.MaxConcurrentEnemies)
                    continue;

                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    TrySpawnEnemy(section, pressure);
                    spawnTimer = section.NextSpawnInterval();
                }
            }

            m_ProgressBar?.SnapTo(endProgress);
        }

        private IEnumerator RunWaveSection(WaveSectionSO section, int markerIndex)
        {
            // The bar itself doesn't move for the whole wave - it's already sitting at this marker's
            // position from the end of the previous cooldown. The marker pulsates to show it's active.
            _inWave = true;
            m_ProgressBar?.SetMarkerActive(markerIndex);

            int spawned = 0;
            float spawnTimer = 0f;

            while (spawned < section.EnemyCount)
            {
                yield return null;

                if (_activeEnemies.Count >= section.MaxConcurrentEnemies)
                    continue;

                spawnTimer -= Time.deltaTime;
                if (spawnTimer > 0f)
                    continue;

                int burst = Mathf.Min(section.SpawnBurstSize, section.EnemyCount - spawned,
                    section.MaxConcurrentEnemies - _activeEnemies.Count);

                // Pressure ramps across the wave itself (first enemy easiest, last hardest), so a wave
                // also builds instead of being a flat random dump. No threat-budget gate here - waves
                // are meant to be relentless and always spawn their full count.
                float pressure = section.EnemyCount > 1 ? (float)spawned / (section.EnemyCount - 1) : 1f;

                for (int i = 0; i < burst; i++)
                    TrySpawnEnemy(section, pressure);

                spawned += burst;
                spawnTimer = section.NextSpawnInterval();
            }

            // Gate: don't advance until every enemy in the arena is dead ("wait till the arena is cleared").
            while (_activeEnemies.Count > 0)
                yield return null;

            // Cleared - marker flips to a checkmark; the bar resumes moving on the next cooldown.
            _inWave = false;
            m_ProgressBar?.SetMarkerCleared(markerIndex);
        }

        private void TrySpawnEnemy(SpawnSectionSO section, float pressure)
        {
            var prefab = section.PickWeightedEnemy(pressure);
            if (prefab == null)
            {
                Debug.LogWarning($"[ArenaDirector] Section '{section.name}' has an empty enemy bucket.");
                return;
            }

            TrySpawnPrefab(prefab);
        }

        /// <summary>
        /// Finds a clear position and spawns <paramref name="prefab"/> there (with the spawn VFX).
        /// Shared by combat-section spawning and the reward director. Returns false if no clear spot
        /// was found in a few attempts.
        /// </summary>
        private bool TrySpawnPrefab(EnemyController prefab)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 spawnPosition = PickRandomPosition();
                if (!IsPositionClear(spawnPosition))
                    continue;

                StartCoroutine(SpawnEnemyWithEffect(prefab, spawnPosition));
                return true;
            }

            Debug.Log("[ArenaDirector] Couldn't find a valid spawn position this attempt.");
            return false;
        }

        /// <summary>
        /// Runs alongside the section loop, spawning reward enemies independently of the combat
        /// difficulty system. Gems are paced against arena progress to hit the config's target count
        /// (and, since the bar only advances during cooldowns, they naturally land in calmer moments);
        /// hearts spawn only while the player is below max health, capped and interval-gated.
        /// </summary>
        private IEnumerator RunRewardDirector()
        {
            var rewards = m_Config.Rewards;
            if (rewards == null)
                yield break;

            int gemsSpawned = 0;
            int heartsSpawned = 0;
            float lastGemTime = -999f;

            float hurtTimer = 0f;
            float postSpawnCooldown = 0f;

            while (_arenaRunning)
            {
                yield return null;

                // Gems: budget-driven. Spawn only during cooldowns (not mid-wave), and only as many as
                // the arena's progress has "unlocked" so far, so the target count is spread across the run.
                if (rewards.GemDropperPrefab != null
                    && gemsSpawned < rewards.TargetGemDropperCount
                    && !_inWave
                    && Time.time - lastGemTime >= rewards.GemMinInterval)
                {
                    int dueByNow = Mathf.CeilToInt(rewards.TargetGemDropperCount * _arenaProgress);
                    if (gemsSpawned < dueByNow && TrySpawnPrefab(rewards.GemDropperPrefab))
                    {
                        gemsSpawned++;
                        lastGemTime = Time.time;
                    }
                }

                // Hearts: hurt-duration gated. The player must stay hurt for a sustained period
                // before a heart spawns - longer when barely hurt, shorter when critical. After
                // spawning, a post-spawn cooldown freezes the timer so hearts never cluster.
                if (rewards.HeartDropperPrefab != null && heartsSpawned < rewards.MaxHeartDroppers)
                {
                    float missingRatio = MissingHealthRatio();
                    if (missingRatio <= 0f)
                    {
                        hurtTimer = 0f;
                    }
                    else if (postSpawnCooldown > 0f)
                    {
                        postSpawnCooldown -= Time.deltaTime;
                    }
                    else
                    {
                        hurtTimer += Time.deltaTime;
                        float requiredDelay = Mathf.Lerp(
                            rewards.BarelyHurtDelay, rewards.CriticalDelay, missingRatio);
                        if (hurtTimer >= requiredDelay && TrySpawnPrefab(rewards.HeartDropperPrefab))
                        {
                            heartsSpawned++;
                            hurtTimer = 0f;
                            postSpawnCooldown = rewards.PostSpawnCooldown;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 0 = full health, 1 = last life. Used to lerp between BarelyHurtDelay and CriticalDelay.
        /// </summary>
        private static float MissingHealthRatio()
        {
            var player = PlayerController.Local;
            if (player == null || player.PlayerHealth == null)
                return 0f;
            int max = player.PlayerHealth.MaxHealth;
            if (max <= 1) return 0f;
            int missing = max - player.PlayerHealth.CurrentHealth;
            if (missing <= 0) return 0f;
            return Mathf.Clamp01((float)missing / (max - 1));
        }

        /// <summary>
        /// Returns true if <paramref name="position"/> is at least <see cref="m_EnemySpacing"/> away
        /// from every currently-tracked active enemy. Uses the live enemy list rather than a Physics2D
        /// overlap check, which would spuriously hit arena walls, the ground collider, and the player.
        /// </summary>
        private bool IsPositionClear(Vector3 position)
        {
            foreach (var enemy in _activeEnemies)
            {
                if (enemy == null) continue;
                if (Vector2.Distance(enemy.transform.position, position) < m_EnemySpacing)
                    return false;
            }
            return true;
        }

        private IEnumerator SpawnEnemyWithEffect(EnemyController prefab, Vector3 spawnPosition)
        {
            ObjectPoolManager.Instance.SpawnItem(PoolableItemType.EnemySpawnParticles, spawnPosition, Quaternion.identity);
            yield return new WaitForSeconds(m_SpawnEffectDelay);

            var enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
            enemy.transform.parent = transform;
            _activeEnemies.Add(enemy);
        }

        private void HandleEnemyDied(EnemyController enemy) => _activeEnemies.Remove(enemy);

        /// <summary>Sum of every currently-alive enemy's ThreatCost - the live "how dangerous is the screen right now" reading.</summary>
        private float CurrentThreat()
        {
            float total = 0f;
            foreach (var enemy in _activeEnemies)
                if (enemy != null)
                    total += enemy.ThreatCost;
            return total;
        }

        private static Vector3 PickRandomPosition()
        {
            float x = UnityEngine.Random.Range(-Constants.EnvironmentConstants.SpawnWidth, Constants.EnvironmentConstants.SpawnWidth);
            float y = UnityEngine.Random.Range(-Constants.EnvironmentConstants.SpawnHeight, Constants.EnvironmentConstants.SpawnHeight);
            return new Vector3(x, y, 0);
        }
    }
}
