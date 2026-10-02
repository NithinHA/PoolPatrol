using System;
using System.Collections.Generic;
using Player;
using SpawningLogic;
using UI.Shop;
using UnityEngine;

namespace Abilities.Goddess
{
    /// <summary>
    /// Decides when and where the Pool Goddess surfaces, and owns the encounter's whole lifecycle
    /// (doc §3-§7, §25):
    ///
    /// <code>
    ///   scheduled progress reached
    ///        - spawn in the central region, away from the player
    ///   PoolGoddess.Appear(window)
    ///        - player touches her  -> pause + open shop
    ///        - window expires      -> retreat, no penalty
    ///   player exits the shop
    ///        - unpause + re-entry grace + retreat
    /// </code>
    ///
    /// Visits are scheduled against the <see cref="ArenaDirector"/> weighted progress rather than a
    /// wall clock, so they land at consistent points in the fight regardless of how fast the player
    /// clears waves. Jitter plus a minimum real-time gap keeps the exact moment hidden (doc §4).
    ///
    /// Put this on its own GameObject in the Game scene and wire the goddess prefab, the arena
    /// director and the shop UI.
    /// </summary>
    public class GoddessEncounterDirector : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Prefab with a PoolGoddess component. Assets/Prefabs/AbilitySelection/PoolGoddess.")]
        [SerializeField] private PoolGoddess m_GoddessPrefab;

        [Tooltip("Drives arena progress. Leave empty to auto-find the one in the scene.")]
        [SerializeField] private ArenaDirector m_ArenaDirector;

        [Tooltip("The ShopScreen panel. Leave empty to auto-find the one in the scene.")]
        [SerializeField] private AbilityShopUI m_ShopUI;

        [Header("Spawn region (doc §3)")]
        [Tooltip("Fraction of the arena half-extents she may spawn within. 0.5 = the central 50%.")]
        [Range(0.1f, 1f)] [SerializeField] private float m_CentralRegionFraction = 0.55f;

        [Tooltip("She never surfaces closer than this to the player, so reaching her is always a trip.")]
        [Min(0f)] [SerializeField] private float m_MinDistanceFromPlayer = 3.5f;

        [Tooltip("Placement attempts before falling back to the best candidate found.")]
        [Min(1)] [SerializeField] private int m_PlacementAttempts = 12;

        [Header("Fallback config")]
        [Tooltip("Used when the arena director has no config (standalone scene testing).")]
        [SerializeField] private ArenaSpawnConfigSO.GoddessEncounters m_FallbackSettings = new();

        /// <summary>Raised when an encounter starts (she has surfaced).</summary>
        public event Action<PoolGoddess> OnEncounterStarted;

        /// <summary>Raised once an encounter is fully over, whether taken or ignored.</summary>
        public event Action OnEncounterEnded;

        /// <summary>Scheduled visit positions on the 0..1 progress timeline, consumed in order.</summary>
        private readonly List<float> _schedule = new();

        private ArenaSpawnConfigSO.GoddessEncounters _settings;
        private PoolGoddess _active;
        private int _nextVisitIndex;
        private float _lastVisitEndTime = float.NegativeInfinity;
        private bool _ready;

        /// <summary>The goddess currently in the water, or null between encounters.</summary>
        public PoolGoddess ActiveGoddess => _active;

        private void Awake()
        {
            if (m_ArenaDirector == null)
                m_ArenaDirector = FindAnyObjectByType<ArenaDirector>();
            if (m_ShopUI == null)
                m_ShopUI = FindAnyObjectByType<AbilityShopUI>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            if (m_ArenaDirector != null)
                m_ArenaDirector.OnArenaBegan += BuildSchedule;
        }

        private void OnDisable()
        {
            if (m_ArenaDirector != null)
                m_ArenaDirector.OnArenaBegan -= BuildSchedule;
        }

        private void Start()
        {
            // The arena director may already have begun during its own Start(), in which case the
            // OnArenaBegan subscription above missed the event.
            if (!_ready && m_ArenaDirector != null && m_ArenaDirector.IsRunning)
                BuildSchedule();
        }

        private void OnDestroy()
        {
            // Never leave the game frozen because the scene was torn down mid-shop.
            if (m_ShopUI != null && m_ShopUI.IsOpen)
                m_ShopUI.ForceClose();
        }

        /// <summary>
        /// Lays out where on the progress bar each visit should happen. Visits are spread evenly
        /// across the run (never at the very start or the very end) and then jittered.
        /// </summary>
        private void BuildSchedule()
        {
            _settings = m_ArenaDirector != null && m_ArenaDirector.Config != null
                ? m_ArenaDirector.Config.Goddess
                : m_FallbackSettings;

            _schedule.Clear();
            _nextVisitIndex = 0;
            _ready = true;

            if (_settings == null || _settings.VisitCount <= 0)
                return;

            if (m_GoddessPrefab == null)
            {
                Debug.LogError("[GoddessEncounterDirector] No goddess prefab assigned - she will never appear.");
                return;
            }

            int count = _settings.VisitCount;
            for (int i = 0; i < count; i++)
            {
                float even = (i + 1f) / (count + 1f);
                float jitter = UnityEngine.Random.Range(-_settings.ScheduleJitter, _settings.ScheduleJitter);
                _schedule.Add(Mathf.Clamp(even + jitter, 0.05f, 0.95f));
            }

            _schedule.Sort();
        }

        private void Update()
        {
            if (!_ready || _active != null || _nextVisitIndex >= _schedule.Count)
                return;
            if (m_ArenaDirector == null || !m_ArenaDirector.IsRunning)
                return;
            if (m_ArenaDirector.ArenaProgress < _schedule[_nextVisitIndex])
                return;
            if (Time.time - _lastVisitEndTime < _settings.MinSecondsBetweenVisits)
                return;

            _nextVisitIndex++;
            SpawnGoddess();
        }

        private void SpawnGoddess()
        {
            Vector3 position = PickSpawnPosition();

            _active = Instantiate(m_GoddessPrefab, position, Quaternion.identity, transform);
            _active.OnTouched += OnGoddessTouched;
            _active.OnRetreated += OnGoddessRetreated;
            _active.Appear(_settings.AvailabilityWindow);

            OnEncounterStarted?.Invoke(_active);
        }

        /// <summary>
        /// Picks a spot inside the central region that is far enough from the player. Falls back to
        /// the furthest candidate tried rather than giving up, so a player parked in the middle
        /// never blocks a scheduled visit outright.
        /// </summary>
        private Vector3 PickSpawnPosition()
        {
            float halfWidth  = Constants.EnvironmentConstants.SpawnWidth  * m_CentralRegionFraction;
            float halfHeight = Constants.EnvironmentConstants.SpawnHeight * m_CentralRegionFraction;

            PlayerController player = PlayerController.Local;
            bool hasPlayer = player != null;
            Vector2 playerPos = hasPlayer ? (Vector2)player.transform.position : Vector2.zero;

            Vector3 best = Vector3.zero;
            float bestDistance = -1f;

            for (int i = 0; i < m_PlacementAttempts; i++)
            {
                var candidate = new Vector3(
                    UnityEngine.Random.Range(-halfWidth, halfWidth),
                    UnityEngine.Random.Range(-halfHeight, halfHeight),
                    0f);

                if (!hasPlayer)
                    return candidate;

                float distance = Vector2.Distance(candidate, playerPos);
                if (distance >= m_MinDistanceFromPlayer)
                    return candidate;

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private void OnGoddessTouched(PoolGoddess goddess)
        {
            goddess.EnterShop();

            if (m_ShopUI == null)
            {
                Debug.LogError("[GoddessEncounterDirector] No AbilityShopUI assigned - nothing to open.");
                goddess.Retreat();
                return;
            }

            m_ShopUI.Open(_settings, OnShopClosed);
        }

        private void OnShopClosed()
        {
            // Re-entry grace: the board may have changed a lot while the game was frozen, so give
            // the player a few seconds to read it before anything can hurt them.
            PlayerController player = PlayerController.Local;
            if (player != null && player.PlayerHealth != null)
                player.PlayerHealth.GrantDamageImmunity(_settings.ReentryGraceSeconds);

            if (_active != null)
                _active.Retreat();
        }

        private void OnGoddessRetreated(PoolGoddess goddess)
        {
            goddess.OnTouched -= OnGoddessTouched;
            goddess.OnRetreated -= OnGoddessRetreated;

            if (_active == goddess)
                _active = null;

            _lastVisitEndTime = Time.time;
            Destroy(goddess.gameObject);

            OnEncounterEnded?.Invoke();
        }
    }
}
