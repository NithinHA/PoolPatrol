using System;
using DG.Tweening;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// One level entry in the <see cref="LevelSelectionScreen"/>. Horizontal (no carousel): an outline
    /// frame marks the selected level, a lock overlay marks locked levels, and an optional star row
    /// shows progress.
    /// </summary>
    public class LevelCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_NumberText;
        [Tooltip("Outline/frame shown while this level is the selected one.")]
        [SerializeField] private GameObject m_Highlight;
        [Tooltip("Overlay shown when the level is locked.")]
        [SerializeField] private GameObject m_Locked;
        [Tooltip("Optional star icons, filled left-to-right by the rating.")]
        [SerializeField] private Image[] m_Stars;
        [SerializeField] private Button m_Button;

        [Header("Progress (optional)")]
        [Tooltip("All-time best score. Hidden until the level has been completed.")]
        [SerializeField] private TMP_Text m_BestScoreText;
        [Tooltip("All-time best streak. Hidden until the level has been completed.")]
        [SerializeField] private TMP_Text m_BestStreakText;

        private Tween _punchTween;
        public bool Unlocked { get; private set; }

        public void Init(int levelNumber, bool unlocked, int starCount, Action onClicked)
        {
            Unlocked = unlocked;

            if (m_NumberText != null) m_NumberText.text = levelNumber.ToString();
            if (m_Locked != null) m_Locked.SetActive(!unlocked);
            SetStars(starCount);

            if (m_Button != null)
            {
                m_Button.onClick.RemoveAllListeners();
                m_Button.interactable = unlocked;
                if (unlocked && onClicked != null)
                    m_Button.onClick.AddListener(() => onClicked());
            }

            if (m_Highlight != null) m_Highlight.SetActive(false);
        }

        /// <summary>Shows the level's all-time bests. Call after <see cref="Init"/>.</summary>
        public void SetProgress(LevelProgress progress)
        {
            if (m_BestScoreText != null)
            {
                m_BestScoreText.gameObject.SetActive(progress.Completed);
                m_BestScoreText.text = $"Best {progress.BestScore}";
            }

            if (m_BestStreakText != null)
            {
                m_BestStreakText.gameObject.SetActive(progress.Completed);
                m_BestStreakText.text = $"Streak {progress.BestStreak}";
            }
        }

        public void SetSelected(bool selected)
        {
            if (m_Highlight != null) m_Highlight.SetActive(selected && Unlocked);

            if (selected && Unlocked)
            {
                _punchTween?.Kill(true);
                _punchTween = transform.DOPunchScale(Vector3.one * 0.06f, 0.25f, 6, 0.6f);
            }
        }

        private void SetStars(int starCount)
        {
            if (m_Stars == null) return;
            for (int i = 0; i < m_Stars.Length; i++)
                if (m_Stars[i] != null) m_Stars[i].enabled = i < starCount;
        }

        private void OnDestroy() => _punchTween?.Kill();
    }
}
