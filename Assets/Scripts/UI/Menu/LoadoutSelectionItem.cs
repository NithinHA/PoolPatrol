using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// One thumbnail slot inside a <see cref="LoadoutSelectorUI"/> strip. Shows an item's icon and a
    /// highlight when it is the centered (selected) slot.
    /// </summary>
    public class LoadoutSelectionItem : MonoBehaviour
    {
        [SerializeField] private Image m_Icon;
        [SerializeField] private GameObject m_Highlight;
        [SerializeField] private Button m_Button;

        public void Set(Sprite icon, bool selected, Action onClicked)
        {
            gameObject.SetActive(true);

            if (m_Icon != null)
            {
                m_Icon.enabled = icon != null;
                m_Icon.sprite = icon;
            }

            if (m_Highlight != null) m_Highlight.SetActive(selected);

            if (m_Button != null)
            {
                m_Button.onClick.RemoveAllListeners();
                if (onClicked != null) m_Button.onClick.AddListener(() => onClicked());
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
