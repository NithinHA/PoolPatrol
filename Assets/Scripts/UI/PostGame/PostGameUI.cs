using System.Text;
using DG.Tweening;
using Economy;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.PostGame
{
    /// <summary>
    /// End-of-level panel shown by <see cref="LevelManager"/> on win or loss. Shows the run's stats
    /// (score, highest streak, coins earned with their breakdown) and offers Retry (reloads the Game
    /// scene with the same arena/level from <see cref="GameSession"/>) or Main Menu.
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
        [SerializeField] private GameObject m_Root;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private Button m_RetryButton;
        [SerializeField] private Button m_MainMenuButton;

        [Header("Run stats (optional)")]
        [SerializeField] private TMP_Text m_ScoreText;
        [SerializeField] private TMP_Text m_StreakText;
        [SerializeField] private TMP_Text m_CoinsText;
        [Tooltip("Where the coins came from: conversion, bonuses, pouches.")]
        [SerializeField] private TMP_Text m_CoinBreakdownText;

        [Header("Count-up")]
        [SerializeField] private float m_CountDuration = 0.8f;

        private Sequence _countSequence;

        private void Awake()
        {
            m_RetryButton.onClick.AddListener(Retry);
            m_MainMenuButton.onClick.AddListener(GoToMainMenu);
            m_Root.SetActive(false);
        }

        private void OnDestroy() => _countSequence?.Kill();

        public void Show(bool won, RunSummary summary)
        {
            m_TitleText.text = won ? "Level Complete!" : "Game Over";

            if (m_StreakText != null)
                m_StreakText.text = $"Highest Streak  {summary.HighestStreak}";
            if (m_CoinBreakdownText != null)
                m_CoinBreakdownText.text = Breakdown(summary);

            m_Root.SetActive(true);
            CountUp(summary);
        }

        /// <summary>Rolls score and coins up from zero. Unscaled, since the run has just ended.</summary>
        private void CountUp(RunSummary summary)
        {
            _countSequence?.Kill();
            SetScore(0, summary.NewBestScore);
            SetCoins(0);

            int score = 0, coins = 0;
            _countSequence = DOTween.Sequence().SetUpdate(true)
                .Append(DOTween.To(() => score, v => SetScore(score = v, summary.NewBestScore), summary.Score, m_CountDuration))
                .Append(DOTween.To(() => coins, v => SetCoins(coins = v), summary.CoinsEarned, m_CountDuration * 0.6f));
        }

        private void SetScore(int value, bool newBest)
        {
            if (m_ScoreText != null)
                m_ScoreText.text = newBest ? $"Score  {value}   NEW BEST!" : $"Score  {value}";
        }

        private void SetCoins(int value)
        {
            if (m_CoinsText != null)
                m_CoinsText.text = $"Coins Earned  +{value}";
        }

        private static string Breakdown(RunSummary summary)
        {
            RunPayout payout = summary.Payout;
            var sb = new StringBuilder();
            AppendLine(sb, summary.Won ? "Score" : "Score (reduced)", payout.ScoreConversion);
            AppendLine(sb, "Level clear", payout.LevelClearBonus);
            AppendLine(sb, "First clear", payout.FirstClearBonus);
            AppendLine(sb, "Arena complete", payout.ArenaCompletionBonus);
            AppendLine(sb, "Coin pouches", summary.PouchCoins);
            return sb.ToString().TrimEnd();
        }

        private static void AppendLine(StringBuilder sb, string label, int amount)
        {
            if (amount > 0)
                sb.Append(label).Append("  +").Append(amount).Append('\n');
        }

        private void Retry()
        {
            ServiceLocator.GetService<ISceneService>().LoadScene(Constants.SceneNames.GAME);
        }

        private void GoToMainMenu()
        {
            ServiceLocator.GetService<ISceneService>().LoadScene(Constants.SceneNames.MENU);
        }
    }
}
