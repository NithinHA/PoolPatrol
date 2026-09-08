using System;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Popup listing the up-to-5 levels of one arena. Shown by <see cref="MenuFlowController"/> after
    /// the player picks an arena in <see cref="ArenaSelectionUI"/>.
    /// </summary>
    public class LevelSelectionUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_ArenaTitleText;
        [SerializeField] private Transform m_LevelButtonContainer;
        [SerializeField] private LevelButtonUI m_LevelButtonPrefab;
        [SerializeField] private Button m_BackButton;

        private Action<int, int> _onLevelChosen;
        private int _arenaIndex;

        public void Bind(Action<int, int> onLevelChosen, Action onBack)
        {
            _onLevelChosen = onLevelChosen;

            m_BackButton.onClick.RemoveAllListeners();
            m_BackButton.onClick.AddListener(() => onBack());
        }

        /// <summary>Populates the level list for the given arena. Call whenever this popup is opened.</summary>
        public void Show(int arenaIndex)
        {
            _arenaIndex = arenaIndex;

            for (int i = m_LevelButtonContainer.childCount - 1; i >= 0; i--)
                Destroy(m_LevelButtonContainer.GetChild(i).gameObject);

            var progression = ServiceLocator.GetProgressionService();
            if (progression == null)
                return;

            var arenaDef = progression.GetArenaDefinition(arenaIndex);
            m_ArenaTitleText.text = arenaDef.ArenaName;

            for (int l = 0; l < arenaDef.LevelCount; l++)
            {
                bool unlocked = progression.IsLevelUnlocked(arenaIndex, l);
                bool completed = progression.IsLevelCompleted(arenaIndex, l);

                var button = Instantiate(m_LevelButtonPrefab, m_LevelButtonContainer);
                int levelIndex = l;
                button.Init(levelIndex + 1, unlocked, completed, () => _onLevelChosen?.Invoke(arenaIndex, levelIndex));
            }
        }
    }
}
