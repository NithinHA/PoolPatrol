using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// The pill at the bottom of every Shop card. It is a live button for coin and real-money prices,
    /// and plain information ("Owned", a locked arena's name) otherwise, with its raycast turned off so those
    /// states can't be tapped at all.
    /// </summary>
    public class ShopPriceTag : MonoBehaviour
    {
        [SerializeField] private Button m_Button;
        [SerializeField] private Image m_Background;
        [SerializeField] private TMP_Text m_Label;

        [Header("Icons")]
        [SerializeField] private GameObject m_CoinIcon;
        [SerializeField] private GameObject m_CheckIcon;
        [SerializeField] private GameObject m_LockIcon;

        [Header("Colours")]
        [SerializeField] private Color m_CoinColor = new(0.11f, 0.45f, 0.62f);
        [SerializeField] private Color m_MoneyColor = new(0.17f, 0.66f, 0.27f);
        [SerializeField] private Color m_OwnedColor = new(0.78f, 0.78f, 0.7f);
        [SerializeField] private Color m_InfoColor = new(0.93f, 0.84f, 0.66f);
        [SerializeField] private Color m_LightTextColor = Color.white;
        [SerializeField] private Color m_DarkTextColor = new(0.36f, 0.28f, 0.18f);

        private Action _onClick;

        private void Awake()
        {
            if (m_Button != null) m_Button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void ShowCoins(int price, Action onClick)
        {
            Apply(price.ToString("N0"), m_CoinColor, m_LightTextColor, m_CoinIcon);
            SetClickable(true, true, onClick);
        }

        /// <param name="price">Ready to display, e.g. the store's localized string.</param>
        /// <param name="available">False greys the button out (store down, another purchase running).</param>
        public void ShowMoney(string price, bool available, Action onClick)
        {
            Apply(price, m_MoneyColor, m_LightTextColor, null);
            SetClickable(true, available, onClick);
        }

        public void ShowOwned()
        {
            Apply("Owned", m_OwnedColor, m_DarkTextColor, m_CheckIcon);
            SetClickable(false, false, null);
        }

        /// <summary>How to earn an item that can't be bought, such as an arena clear.</summary>
        public void ShowInfo(string text)
        {
            Apply(text, m_InfoColor, m_DarkTextColor, m_LockIcon);
            SetClickable(false, false, null);
        }

        private void Apply(string text, Color background, Color textColor, GameObject icon)
        {
            if (m_Label != null)
            {
                m_Label.text = text;
                m_Label.color = textColor;
            }
            if (m_Background != null) m_Background.color = background;

            if (m_CoinIcon != null) m_CoinIcon.SetActive(icon == m_CoinIcon);
            if (m_CheckIcon != null) m_CheckIcon.SetActive(icon == m_CheckIcon);
            if (m_LockIcon != null) m_LockIcon.SetActive(icon == m_LockIcon);
        }

        private void SetClickable(bool isButton, bool interactable, Action onClick)
        {
            _onClick = onClick;
            if (m_Button != null)
            {
                m_Button.enabled = isButton;
                m_Button.interactable = interactable;
            }
            if (m_Background != null) m_Background.raycastTarget = isButton;
        }
    }
}
