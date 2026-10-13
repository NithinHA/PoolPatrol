using System;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Run-scoped scoreboard: how long the current run has lasted, how many enemies the player has
    /// killed, and which wave of the arena they are on. Reset at the start of every run by
    /// <c>LevelManager</c> alongside the economy / ability / run-modifier services.
    ///
    /// Everything here is "this run only" — the pause menu's Current Run panel and the post-game
    /// screen both read it, so neither has to keep its own counters.
    /// </summary>
    public interface IRunStatsService : IService
    {
        /// <summary>Raised whenever any stat changes. UI that is visible can refresh on this.</summary>
        event Action OnStatsChanged;

        /// <summary>Seconds of *unpaused* play since <see cref="BeginRun"/>, independent of timescale.</summary>
        float ElapsedSeconds { get; }

        /// <summary>Enemies that have died this run, counted from <c>EnemyController.OnAnyEnemyDied</c>.</summary>
        int EnemiesKilled { get; }

        /// <summary>1-based wave the arena is on (or last reached). 0 before the first wave starts.</summary>
        int CurrentWave { get; }

        /// <summary>Total wave sections in the arena config. 0 until the arena reports it.</summary>
        int TotalWaves { get; }

        /// <summary>True between <see cref="BeginRun"/> and <see cref="EndRun"/>.</summary>
        bool IsRunActive { get; }

        /// <summary>Zeroes every counter and starts the clock. Call once per run.</summary>
        void BeginRun();

        /// <summary>Stops the clock. Counters stay readable so the post-game screen can show them.</summary>
        void EndRun();

        /// <summary>
        /// Advances the run clock by <paramref name="unscaledDeltaTime"/>. Driven by
        /// <c>LevelManager</c> so paused time (shop, pause menu) is not counted as play time.
        /// </summary>
        void Tick(float unscaledDeltaTime);

        /// <summary>Reported by <c>ArenaDirector</c> as the timeline walks its wave sections.</summary>
        void SetWaveProgress(int currentWave, int totalWaves);

        /// <summary>Formats <see cref="ElapsedSeconds"/> as <c>MM:SS</c> (or <c>H:MM:SS</c> past an hour).</summary>
        string FormatElapsed();
    }
}
