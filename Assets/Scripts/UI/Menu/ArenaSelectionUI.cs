using System;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Lists every arena from <see cref="IProgression"/>, locked or unlocked, and reports which one
    /// the player picked back up to <see cref="MenuFlowController"/>.
    /// </summary>
    public class ArenaSelectionUI : MonoBehaviour
    {
        [SerializeField] private Transform m_CardContainer;
        [SerializeField] private ArenaCardUI m_CardPrefab;
        [SerializeField] private Button m_BackButton;

        private Action<int> _onArenaChosen;

        public void Bind(Action<int> onArenaChosen, Action onBack)
        {
            _onArenaChosen = onArenaChosen;

            m_BackButton.onClick.RemoveAllListeners();
            m_BackButton.onClick.AddListener(() => onBack());
        }

        /// <summary>Rebuilds the card list from current progression state. Call whenever this panel is shown.</summary>
        public void Refresh()
        {
            for (int i = m_CardContainer.childCount - 1; i >= 0; i--)
                Destroy(m_CardContainer.GetChild(i).gameObject);

            var progression = ServiceLocator.GetProgressionService();
            if (progression == null)
                return;

            for (int i = 0; i < progression.ArenaCount; i++)
            {
                var arenaDef = progression.GetArenaDefinition(i);
                bool unlocked = progression.IsLevelUnlocked(i, 0);

                int completedCount = 0;
                for (int l = 0; l < arenaDef.LevelCount; l++)
                    if (progression.IsLevelCompleted(i, l))
                        completedCount++;

                string status = unlocked
                    ? $"{completedCount}/{arenaDef.LevelCount} Complete"
                    : "Locked";

                var card = Instantiate(m_CardPrefab, m_CardContainer);
                int arenaIndex = i;
                card.Init(arenaDef.ArenaName, unlocked, status, () => _onArenaChosen?.Invoke(arenaIndex));
            }
        }
    }
}
