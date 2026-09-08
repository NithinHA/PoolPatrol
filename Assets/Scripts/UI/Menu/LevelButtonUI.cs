using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>One level entry in the <see cref="LevelSelectionUI"/> popup.</summary>
    public class LevelButtonUI : MonoBehaviour
    {
        [SerializeField] private Button m_SelectButton;
        [SerializeField] private TMP_Text m_LevelNumberText;
        [SerializeField] private GameObject m_LockIcon;
        [SerializeField] private GameObject m_CompletedCheck;

        public void Init(int levelNumber, bool unlocked, bool completed, Action onClicked)
        {
            m_LevelNumberText.text = levelNumber.ToString();
            m_LockIcon.SetActive(!unlocked);
            m_CompletedCheck.SetActive(completed);
            m_SelectButton.interactable = unlocked;

            m_SelectButton.onClick.RemoveAllListeners();
            if (unlocked)
                m_SelectButton.onClick.AddListener(() => onClicked());
        }
    }
}
