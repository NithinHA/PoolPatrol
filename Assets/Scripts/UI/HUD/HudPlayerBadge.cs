using PTL.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    /// <summary>
    /// Static bits of the HUD that come from the menu selection: the player's character portrait in
    /// the avatar bubble and the "LV n" tab above the progress bar. Set once at Start; neither changes
    /// during a run.
    /// </summary>
    public class HudPlayerBadge : MonoBehaviour
    {
        [SerializeField] private Image m_Portrait;
        [SerializeField] private TMP_Text m_LevelText;
        [SerializeField] private string m_LevelFormat = "LV {0}";

        private void Start()
        {
            if (m_LevelText != null)
                m_LevelText.text = string.Format(m_LevelFormat, GameSession.SelectedLevelIndex + 1);

            if (m_Portrait == null)
                return;

            // Keeps the scene's placeholder portrait when the Loadout service is missing
            // (e.g. Game.unity played directly without Bootstrap).
            var character = ServiceLocator.GetLoadoutService()?.SelectedCharacter;
            if (character == null)
                return;

            Sprite portrait = character.HeadSprite != null ? character.HeadSprite : character.Icon;
            if (portrait != null)
                m_Portrait.sprite = portrait;
        }
    }
}
