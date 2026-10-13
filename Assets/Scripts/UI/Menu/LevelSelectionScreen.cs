using System;
using System.Collections.Generic;
using PTL.Framework;
using PTL.Framework.Services;
using SpawningLogic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Second menu screen: the levels of the chosen arena laid out horizontally. The player picks a
    /// level (outline highlight), then Next carries the (arena, level) pair up to the navigator.
    /// </summary>
    public class LevelSelectionScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_ArenaTitleText;
        [SerializeField] private TMP_Text m_ArenaSubtitleText;
        [SerializeField] private Transform m_CardContainer;
        [SerializeField] private LevelCard m_CardPrefab;
        [SerializeField] private Button m_NextButton;
        [SerializeField] private Button m_BackButton;

        private readonly List<LevelCard> _cards = new();
        private Action<int, int> _onNext;
        private Action _onBack;
        private int _arenaIndex;
        private int _selectedLevel = -1;

        public void Bind(Action<int, int> onNext, Action onBack)
        {
            _onNext = onNext;
            _onBack = onBack;

            if (m_NextButton != null)
            {
                m_NextButton.onClick.RemoveAllListeners();
                m_NextButton.onClick.AddListener(() =>
                {
                    if (_selectedLevel >= 0) _onNext?.Invoke(_arenaIndex, _selectedLevel);
                });
            }
            if (m_BackButton != null)
            {
                m_BackButton.onClick.RemoveAllListeners();
                m_BackButton.onClick.AddListener(() => _onBack?.Invoke());
            }
        }

        /// <summary>Rebuilds the level list for the given arena and auto-selects the latest unlocked.</summary>
        public void Show(int arenaIndex)
        {
            _arenaIndex = arenaIndex;

            foreach (LevelCard card in _cards)
                if (card != null) Destroy(card.gameObject);
            _cards.Clear();
            _selectedLevel = -1;

            // Also clear any designer-placed placeholder cards left in the container.
            for (int i = m_CardContainer.childCount - 1; i >= 0; i--)
                Destroy(m_CardContainer.GetChild(i).gameObject);

            IProgression progression = ServiceLocator.GetProgressionService();
            if (progression == null) return;

            ArenaDefinitionSO arena = progression.GetArenaDefinition(arenaIndex);
            if (m_ArenaTitleText != null) m_ArenaTitleText.text = arena.ArenaName;
            if (m_ArenaSubtitleText != null) m_ArenaSubtitleText.text = arena.Description;

            for (int l = 0; l < arena.LevelCount; l++)
            {
                bool unlocked = progression.IsLevelUnlocked(arenaIndex, l);
                bool completed = progression.IsLevelCompleted(arenaIndex, l);

                LevelCard card = Instantiate(m_CardPrefab, m_CardContainer);
                int level = l;
                // No per-level star rating is tracked yet; show all stars lit once completed.
                int stars = completed ? (card != null ? 3 : 0) : 0;
                card.Init(level + 1, unlocked, stars, () => Select(level));
                _cards.Add(card);
            }

            Select(progression.GetHighestUnlockedLevel(arenaIndex));
        }

        private void Select(int level)
        {
            if (level < 0 || level >= _cards.Count || !_cards[level].Unlocked)
            {
                if (m_NextButton != null) m_NextButton.interactable = _selectedLevel >= 0;
                return;
            }

            _selectedLevel = level;
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].SetSelected(i == level);

            if (m_NextButton != null) m_NextButton.interactable = true;
        }
    }
}
