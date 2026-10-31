using UnityEngine;

namespace Economy
{
    /// <summary>
    /// Tuning for the score and coin economy (Docs/economy-and-progression.md). One asset, read by
    /// <see cref="ScoreKeeper"/> while a run is live and by <c>LevelManager</c> when it settles the
    /// run-end payout. All values are starting points — tune here, not in code.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Economy/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfigSO : ScriptableObject
    {
        [Header("Score: per-shot multiplier")]
        [Tooltip("Multiplier added per point of streak at the moment of the shot (the 0.2 in 1 + 0.2·s + 0.5·(n−1)).")]
        [Min(0f)] [SerializeField] private float m_StreakWeight = 0.2f;

        [Tooltip("Streak values above this count as this for scoring. The combo itself keeps climbing.")]
        [Min(0)] [SerializeField] private int m_MaxScoringStreak = 6;

        [Tooltip("Multiplier added per extra enemy killed by the same shot (the 0.5 in 1 + 0.2·s + 0.5·(n−1)).")]
        [Min(0f)] [SerializeField] private float m_MultiKillWeight = 0.5f;

        [Header("Score: kill grouping")]
        [Tooltip("Seconds without a new kill before a shot's kills are scored as one group. A shot that " +
                 "resolves sooner (bullet destroyed, shotgun blast done) is scored straight away.")]
        [Min(0f)] [SerializeField] private float m_ResolutionWindow = 0.3f;

        [Header("Score: pickups")]
        [Tooltip("Flat score for picking up a gem. Not multiplied by streak or multi-kill.")]
        [Min(0)] [SerializeField] private int m_GemPickupScore = 2;

        [Header("Coins: run-end payout")]
        [Tooltip("Score needed for one coin. Payout is floor(score / this).")]
        [Min(1)] [SerializeField] private int m_ScorePerCoin = 50;

        [Tooltip("Share of the score conversion kept on a loss (1 = everything, as on a win).")]
        [Range(0f, 1f)] [SerializeField] private float m_LossConversionRatio = 0.5f;

        [Tooltip("Coins for clearing any level, every time. Win only.")]
        [Min(0)] [SerializeField] private int m_LevelClearBonus = 20;

        [Tooltip("One-time coins the first time a level is cleared. Win only.")]
        [Min(0)] [SerializeField] private int m_FirstClearBonus = 100;

        [Tooltip("One-time coins the first time every level of an arena is cleared. Win only.")]
        [Min(0)] [SerializeField] private int m_ArenaCompletionBonus = 300;

        public float ResolutionWindow => m_ResolutionWindow;
        public int GemPickupScore => m_GemPickupScore;

        /// <summary>
        /// <c>hitScore = Σ baseValue × (1 + streakWeight·s + multiKillWeight·(n − 1))</c>, rounded
        /// to the nearest integer. <paramref name="streak"/> is the streak when the shot was fired,
        /// before that shot's own hit counted.
        /// </summary>
        public int ComputeHitScore(int baseValueSum, int streak, int killCount)
        {
            if (baseValueSum <= 0 || killCount <= 0)
                return 0;

            int s = Mathf.Clamp(streak, 0, m_MaxScoringStreak);
            float multiplier = 1f + m_StreakWeight * s + m_MultiKillWeight * (killCount - 1);
            return Mathf.RoundToInt(baseValueSum * multiplier);
        }

        /// <summary>Coins granted on the run-end screen. Coin pouches are not included: they were credited on pickup.</summary>
        public RunPayout ComputePayout(bool won, int score, LevelMilestones milestones)
        {
            int converted = Mathf.Max(0, score) / m_ScorePerCoin;
            if (!won)
                converted = Mathf.FloorToInt(converted * m_LossConversionRatio);

            return new RunPayout(
                converted,
                won ? m_LevelClearBonus : 0,
                won && milestones.FirstClear ? m_FirstClearBonus : 0,
                won && milestones.ArenaCompleted ? m_ArenaCompletionBonus : 0);
        }
    }

    /// <summary>The one-time progression milestones a win reached.</summary>
    public readonly struct LevelMilestones
    {
        public readonly bool FirstClear;
        public readonly bool ArenaCompleted;

        public LevelMilestones(bool firstClear, bool arenaCompleted)
        {
            FirstClear     = firstClear;
            ArenaCompleted = arenaCompleted;
        }
    }

    /// <summary>Breakdown of the coins granted on the run-end screen.</summary>
    public readonly struct RunPayout
    {
        public readonly int ScoreConversion;
        public readonly int LevelClearBonus;
        public readonly int FirstClearBonus;
        public readonly int ArenaCompletionBonus;

        public int Total => ScoreConversion + LevelClearBonus + FirstClearBonus + ArenaCompletionBonus;

        public RunPayout(int scoreConversion, int levelClearBonus, int firstClearBonus, int arenaCompletionBonus)
        {
            ScoreConversion      = scoreConversion;
            LevelClearBonus      = levelClearBonus;
            FirstClearBonus      = firstClearBonus;
            ArenaCompletionBonus = arenaCompletionBonus;
        }
    }

    /// <summary>Everything the post-game screen shows about the run that just ended.</summary>
    public readonly struct RunSummary
    {
        public readonly bool Won;
        public readonly int Score;
        public readonly int HighestStreak;
        public readonly RunPayout Payout;

        /// <summary>Coins from pouches picked up mid-run (already credited when picked up).</summary>
        public readonly int PouchCoins;

        public readonly bool NewBestScore;

        /// <summary>All coins this run earned: the payout plus pouch pickups.</summary>
        public int CoinsEarned => Payout.Total + PouchCoins;

        public RunSummary(bool won, int score, int highestStreak, RunPayout payout, int pouchCoins, bool newBestScore)
        {
            Won           = won;
            Score         = score;
            HighestStreak = highestStreak;
            Payout        = payout;
            PouchCoins    = pouchCoins;
            NewBestScore  = newBestScore;
        }
    }
}
