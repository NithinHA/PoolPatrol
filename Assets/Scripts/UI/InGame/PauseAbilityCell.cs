using Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGame
{
    /// <summary>
    /// One tile in the pause menu's "Active Abilities" grid: the ability's icon, plus a small corner
    /// bubble showing the owned level. The bubble is only shown for stackable abilities (those with
    /// more than one level) — a one-shot upgrade is always "level 1" and a "1" badge on it is noise.
    ///
    /// Spawned under <c>AbilitiesBox</c> by <see cref="PauseMenuUI"/> and pooled by simple
    /// activate/deactivate, so cells survive repeated opens without reallocating.
    /// </summary>
    public class PauseAbilityCell : MonoBehaviour
    {
        [Tooltip("The ability's icon. Stretched inside the cell background.")]
        [SerializeField] private Image m_Icon;

        [Tooltip("Corner badge shown only when the ability has more than one level.")]
        [SerializeField] private GameObject m_LevelBubble;

        [SerializeField] private TMP_Text m_LevelText;

        /// <summary>The ability this cell is currently showing. Null when the cell is unused.</summary>
        public AbilityDefinition Ability { get; private set; }

        public void Bind(AbilityDefinition ability, int level)
        {
            Ability = ability;

            if (ability == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (m_Icon != null)
            {
                m_Icon.sprite = ability.Icon;
                // A missing icon would otherwise draw as a white box over the cell background.
                m_Icon.enabled = ability.Icon != null;
            }

            bool showLevel = ability.IsStackable && level > 0;
            if (m_LevelBubble != null)
                m_LevelBubble.SetActive(showLevel);
            if (showLevel && m_LevelText != null)
                m_LevelText.text = level.ToString();

            gameObject.SetActive(true);
        }
    }
}
