using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>One arena entry in the <see cref="ArenaSelectionUI"/> list.</summary>
    public class ArenaCardUI : MonoBehaviour
    {
        [SerializeField] private Button m_SelectButton;
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private TMP_Text m_StatusText;
        [SerializeField] private GameObject m_LockIcon;

        public void Init(string arenaName, bool unlocked, string statusText, Action onClicked)
        {
            m_NameText.text = arenaName;
            m_StatusText.text = statusText;
            m_LockIcon.SetActive(!unlocked);
            m_SelectButton.interactable = unlocked;

            m_SelectButton.onClick.RemoveAllListeners();
            if (unlocked)
                m_SelectButton.onClick.AddListener(() => onClicked());
        }
    }
}
