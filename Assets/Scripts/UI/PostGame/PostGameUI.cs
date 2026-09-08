using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.PostGame
{
    /// <summary>
    /// End-of-level panel shown by <see cref="LevelManager"/> on win or loss. Offers Retry (reloads
    /// the Game scene with the same arena/level from <see cref="GameSession"/>) or Main Menu.
    /// </summary>
    public class PostGameUI : MonoBehaviour
    {
        [SerializeField] private GameObject m_Root;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private Button m_RetryButton;
        [SerializeField] private Button m_MainMenuButton;

        private void Awake()
        {
            m_RetryButton.onClick.AddListener(Retry);
            m_MainMenuButton.onClick.AddListener(GoToMainMenu);
            m_Root.SetActive(false);
        }

        public void Show(bool won)
        {
            m_TitleText.text = won ? "Level Complete!" : "Game Over";
            m_Root.SetActive(true);
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
