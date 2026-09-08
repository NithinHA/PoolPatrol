using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Owns navigation between the three Menu-scene panels (Main Menu -> Arena Selection ->
    /// Level Selection) and kicks off the Game scene load once a level is picked.
    /// </summary>
    public class MenuFlowController : MonoBehaviour
    {
        [SerializeField] private GameObject m_MainMenuPanel;
        [SerializeField] private Button m_PlayButton;

        [SerializeField] private GameObject m_ArenaSelectionPanel;
        [SerializeField] private ArenaSelectionUI m_ArenaSelectionUI;

        [SerializeField] private GameObject m_LevelSelectionPanel;
        [SerializeField] private LevelSelectionUI m_LevelSelectionUI;

        private void Awake()
        {
            m_PlayButton.onClick.AddListener(ShowArenaSelection);
            m_ArenaSelectionUI.Bind(ShowLevelSelection, ShowMainMenu);
            m_LevelSelectionUI.Bind(StartLevel, ShowArenaSelection);

            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            m_MainMenuPanel.SetActive(true);
            m_ArenaSelectionPanel.SetActive(false);
            m_LevelSelectionPanel.SetActive(false);
        }

        private void ShowArenaSelection()
        {
            m_MainMenuPanel.SetActive(false);
            m_ArenaSelectionPanel.SetActive(true);
            m_LevelSelectionPanel.SetActive(false);
            m_ArenaSelectionUI.Refresh();
        }

        private void ShowLevelSelection(int arenaIndex)
        {
            m_LevelSelectionPanel.SetActive(true);
            m_LevelSelectionUI.Show(arenaIndex);
        }

        private void StartLevel(int arenaIndex, int levelIndex)
        {
            GameSession.SelectedArenaIndex = arenaIndex;
            GameSession.SelectedLevelIndex = levelIndex;
            ServiceLocator.GetService<ISceneService>().LoadScene(Constants.SceneNames.GAME);
        }
    }
}
