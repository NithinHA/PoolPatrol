using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>One tab along the top of the Shop: cream while active, light blue otherwise.</summary>
    public class ShopTabButton : MonoBehaviour
    {
        [SerializeField] private Button m_Button;
        [SerializeField] private Image m_Background;
        [SerializeField] private TMP_Text m_Label;

        [SerializeField] private Sprite m_ActiveSprite;
        [SerializeField] private Sprite m_InactiveSprite;
        // [SerializeField] private Color m_ActiveColor = new(1f, 0.93f, 0.68f);
        // [SerializeField] private Color m_InactiveColor = new(0.68f, 0.88f, 0.98f);
        [SerializeField] private Color m_ActiveTextColor = new(0.36f, 0.2f, 0.08f);
        [SerializeField] private Color m_InactiveTextColor = new(0.1f, 0.25f, 0.42f);

        public void Bind(Action onClick)
        {
            if (m_Button == null) return;
            m_Button.onClick.RemoveAllListeners();
            m_Button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void SetSelected(bool selected)
        {
            if (m_Background != null) m_Background.sprite = selected ? m_ActiveSprite : m_InactiveSprite;
            if (m_Label != null) m_Label.color = selected ? m_ActiveTextColor : m_InactiveTextColor;
        }
    }
}
