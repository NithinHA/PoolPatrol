using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// One thumbnail slot inside a <see cref="LoadoutSelectorUI"/> strip. Shows an item's icon, a
    /// locked overlay with a short tag (coin price, "Arena 2", "Store"), and a highlight when it is
    /// the centered (selected) slot. Locked slots stay clickable so the player can see how to unlock.
    /// </summary>
    public class LoadoutSelectionItem : MonoBehaviour
    {
        [SerializeField] private Image m_Icon;
        [SerializeField] private GameObject m_Highlight;
        [SerializeField] private GameObject m_Locked;
        [SerializeField] private Button m_Button;

        [Tooltip("Optional: shown on locked slots — the coin price or what unlocks the item.")]
        [SerializeField] private TMP_Text m_TagText;

        [Tooltip("Tint applied to the icon while the item is locked.")]
        [SerializeField] private Color m_LockedTint = new Color(1f, 1f, 1f, 0.45f);

        public void Set(Sprite icon, bool unlocked, bool selected, Action onClicked, string lockedTag = null)
        {
            gameObject.SetActive(true);

            if (m_Icon != null)
            {
                m_Icon.enabled = icon != null;
                m_Icon.sprite = icon;
                m_Icon.color = unlocked ? Color.white : m_LockedTint;
            }

            if (m_Locked != null) m_Locked.SetActive(!unlocked);
            if (m_Highlight != null) m_Highlight.SetActive(selected && unlocked);

            if (m_TagText != null)
            {
                bool showTag = !unlocked && !string.IsNullOrEmpty(lockedTag);
                m_TagText.gameObject.SetActive(showTag);
                if (showTag) m_TagText.text = lockedTag;
            }

            if (m_Button != null)
            {
                m_Button.onClick.RemoveAllListeners();
                if (onClicked != null) m_Button.onClick.AddListener(() => onClicked());
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
