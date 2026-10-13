using System;
using PTL.Framework;
using PTL.Framework.Services;
using SpawningLogic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// First menu screen: the arena carousel plus the Next button. Owns no transition logic — it just
    /// reports the chosen arena up to the <see cref="MenuNavigator"/>.
    /// </summary>
    public class ArenaSelectionScreen : MonoBehaviour
    {
        [SerializeField] private ArenaCarousel m_Carousel;
        [SerializeField] private Button m_NextButton;

        [Header("Optional screen-level labels (mirror the centred arena)")]
        [SerializeField] private TMP_Text m_ArenaNameText;
        [SerializeField] private TMP_Text m_ArenaDescText;

        private Action<int> _onNext;

        public int SelectedArenaIndex => m_Carousel != null ? m_Carousel.SelectedIndex : 0;

        public void Bind(Action<int> onNext)
        {
            _onNext = onNext;
            if (m_NextButton != null)
            {
                m_NextButton.onClick.RemoveAllListeners();
                m_NextButton.onClick.AddListener(() => _onNext?.Invoke(SelectedArenaIndex));
            }
            if (m_Carousel != null)
                m_Carousel.OnSelectionChanged += UpdateLabels;
        }

        private void OnDestroy()
        {
            if (m_Carousel != null)
                m_Carousel.OnSelectionChanged -= UpdateLabels;
        }

        /// <summary>Rebuilds the carousel, centering on the latest unlocked arena.</summary>
        public void Show()
        {
            IProgression progression = ServiceLocator.GetProgressionService();
            int initial = progression != null ? progression.GetHighestUnlockedArena() : 0;
            if (m_Carousel != null) m_Carousel.Build(initial);
            UpdateLabels(initial);
        }

        private void UpdateLabels(int arenaIndex)
        {
            IProgression progression = ServiceLocator.GetProgressionService();
            if (progression == null || arenaIndex < 0 || arenaIndex >= progression.ArenaCount) return;

            ArenaDefinitionSO arena = progression.GetArenaDefinition(arenaIndex);
            if (m_ArenaNameText != null) m_ArenaNameText.text = arena.ArenaName;
            if (m_ArenaDescText != null) m_ArenaDescText.text = arena.Description;
        }
    }
}
