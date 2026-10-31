using SpawningLogic;

namespace PTL.Framework.Services
{
    /// <summary>Persistent, per-level record. Only a win updates it (best-of comparison).</summary>
    public readonly struct LevelProgress
    {
        public readonly bool Completed;
        public readonly int  BestScore;
        public readonly int  BestStreak;

        public LevelProgress(bool completed, int bestScore, int bestStreak)
        {
            Completed  = completed;
            BestScore  = bestScore;
            BestStreak = bestStreak;
        }
    }

    /// <summary>What a win changed, so the run-end payout can award its one-time bonuses.</summary>
    public readonly struct LevelWinResult
    {
        /// <summary>The level had never been completed before this win.</summary>
        public readonly bool FirstClear;

        /// <summary>This win completed the arena for the first time.</summary>
        public readonly bool ArenaCompleted;

        public readonly bool NewBestScore;
        public readonly bool NewBestStreak;

        public LevelWinResult(bool firstClear, bool arenaCompleted, bool newBestScore, bool newBestStreak)
        {
            FirstClear     = firstClear;
            ArenaCompleted = arenaCompleted;
            NewBestScore   = newBestScore;
            NewBestStreak  = newBestStreak;
        }
    }

    public interface IProgression : IService
    {
        bool IsLevelUnlocked(int arenaIndex, int levelIndex);
        bool IsLevelCompleted(int arenaIndex, int levelIndex);
        bool IsArenaCompleted(int arenaIndex);
        void CompleteLevel(int arenaIndex, int levelIndex);

        /// <summary>
        /// Records a win: marks the level completed and keeps the best score / streak. Returns which
        /// one-time milestones (first clear, arena completion) this win reached.
        /// </summary>
        LevelWinResult RecordLevelWin(int arenaIndex, int levelIndex, int score, int streak);

        LevelProgress GetLevelProgress(int arenaIndex, int levelIndex);

        int GetHighestUnlockedArena();
        int GetHighestUnlockedLevel(int arenaIndex);
        ArenaSpawnConfigSO GetLevelConfig(int arenaIndex, int levelIndex);
        ArenaDefinitionSO GetArenaDefinition(int arenaIndex);
        int ArenaCount { get; }
    }
}
