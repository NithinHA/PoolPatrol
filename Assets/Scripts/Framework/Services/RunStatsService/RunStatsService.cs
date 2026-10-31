using System;
using Enemy;
using UnityEngine;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Default <see cref="IRunStatsService"/>.
    ///
    /// Kills are picked up passively from the static <see cref="EnemyController.OnAnyEnemyDied"/>
    /// event, so nothing in the enemy or spawning code needs to know this service exists. The clock
    /// is pushed in from <c>LevelManager.Update</c> rather than read from <c>Time</c> directly:
    /// that keeps the "stop counting while the game is paused" rule in one place and keeps the
    /// service a plain C# object with no Unity callbacks of its own.
    /// </summary>
    public class RunStatsService : IRunStatsService
    {
        private float _elapsed;
        private int   _enemiesKilled;
        private int   _currentWave;
        private int   _totalWaves;
        private int   _score;
        private int   _highestStreak;
        private int   _coinsCollected;
        private int   _coinPouchesDropped;
        private bool  _runActive;

        public event Action OnStatsChanged;

        public float ElapsedSeconds     => _elapsed;
        public int   EnemiesKilled      => _enemiesKilled;
        public int   CurrentWave        => _currentWave;
        public int   TotalWaves         => _totalWaves;
        public int   Score              => _score;
        public int   HighestStreak      => _highestStreak;
        public int   CoinsCollected     => _coinsCollected;
        public int   CoinPouchesDropped => _coinPouchesDropped;
        public bool  IsRunActive        => _runActive;

#region Default callbacks

        public void Start()
        {
            EnemyController.OnAnyEnemyDied += OnAnyEnemyDied;
        }

        public void OnDestroy()
        {
            EnemyController.OnAnyEnemyDied -= OnAnyEnemyDied;
            OnStatsChanged = null;
        }

#endregion

        public void BeginRun()
        {
            _elapsed            = 0f;
            _enemiesKilled      = 0;
            _currentWave        = 0;
            _totalWaves         = 0;
            _score              = 0;
            _highestStreak      = 0;
            _coinsCollected     = 0;
            _coinPouchesDropped = 0;
            _runActive          = true;
            OnStatsChanged?.Invoke();
        }

        public void EndRun()
        {
            if (!_runActive)
                return;

            _runActive = false;
            OnStatsChanged?.Invoke();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!_runActive || unscaledDeltaTime <= 0f)
                return;

            _elapsed += unscaledDeltaTime;
            // Deliberately silent: the clock changes every frame and no UI wants an event per tick.
            // Readers that show the timer poll ElapsedSeconds while they are visible.
        }

        public void SetWaveProgress(int currentWave, int totalWaves)
        {
            currentWave = Mathf.Max(0, currentWave);
            totalWaves  = Mathf.Max(0, totalWaves);

            if (currentWave == _currentWave && totalWaves == _totalWaves)
                return;

            _currentWave = currentWave;
            _totalWaves  = totalWaves;
            OnStatsChanged?.Invoke();
        }

        public void AddScore(int amount)
        {
            if (!_runActive || amount <= 0)
                return;

            _score += amount;
            OnStatsChanged?.Invoke();
        }

        public void ReportStreak(int streak)
        {
            if (!_runActive || streak <= _highestStreak)
                return;

            _highestStreak = streak;
            OnStatsChanged?.Invoke();
        }

        public void AddCollectedCoins(int amount)
        {
            if (!_runActive || amount <= 0)
                return;

            _coinsCollected += amount;
            OnStatsChanged?.Invoke();
        }

        public void RecordCoinPouchDrop()
        {
            if (!_runActive)
                return;

            _coinPouchesDropped++;
            // Silent: an internal cap counter, not something any UI displays.
        }

        public string FormatElapsed()
        {
            int total   = Mathf.Max(0, Mathf.FloorToInt(_elapsed));
            int hours   = total / 3600;
            int minutes = (total % 3600) / 60;
            int seconds = total % 60;

            return hours > 0
                ? $"{hours}:{minutes:00}:{seconds:00}"
                : $"{minutes:00}:{seconds:00}";
        }

        private void OnAnyEnemyDied(EnemyController enemy)
        {
            if (!_runActive)
                return;

            _enemiesKilled++;
            OnStatsChanged?.Invoke();
        }
    }
}
