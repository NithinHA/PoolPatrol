using System;
using DG.Tweening;
using Loadout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Modal shown when the player taps a locked loadout item: the item, how it unlocks, and — for
    /// coin items — a Buy button that is disabled (showing the shortfall) when the player can't
    /// afford it. Arena-clear and IAP items get an explanation and no Buy button. The panel only
    /// presents; <see cref="LoadoutScreen"/> performs the purchase through the loadout service.
    /// </summary>
    public class LoadoutPurchasePanel : MonoBehaviour
    {
        [SerializeField] private GameObject m_Root;
        [Tooltip("The box that pops in; the root is the full-screen dimmer.")]
        [SerializeField] private RectTransform m_Box;
        [SerializeField] private Image m_Icon;
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private TMP_Text m_ReasonText;
        [SerializeField] private Button m_BuyButton;
        [SerializeField] private TMP_Text m_BuyLabel;
        [Tooltip("Close button and/or the dimmer behind the box.")]
        [SerializeField] private Button[] m_CloseButtons;

        private Action _onBuy;
        private Tween _popTween;

        public bool IsOpen => m_Root != null && m_Root.activeSelf;

        private void Awake()
        {
            if (m_BuyButton != null)
                m_BuyButton.onClick.AddListener(() => _onBuy?.Invoke());
            if (m_CloseButtons != null)
                foreach (Button close in m_CloseButtons)
                    if (close != null) close.onClick.AddListener(Hide);

            if (m_Root != null) m_Root.SetActive(false);
        }

        private void OnDestroy() => _popTween?.Kill();

        /// <param name="reason">How the item unlocks, ready to display.</param>
        /// <param name="balance">Current coin balance, for the affordability check.</param>
        /// <param name="onBuy">Called when Buy is pressed (coin items only).</param>
        public void Show(LoadoutItemSO item, string reason, int balance, Action onBuy)
        {
            if (item == null || m_Root == null) return;

            if (m_Icon != null)
            {
                m_Icon.sprite = item.Icon;
                m_Icon.enabled = item.Icon != null;
            }
            if (m_NameText != null) m_NameText.text = item.DisplayName;
            if (m_ReasonText != null) m_ReasonText.text = reason;

            bool forSale = item.Unlock == UnlockType.Coins;
            bool affordable = balance >= item.CoinPrice;
            _onBuy = forSale && affordable ? onBuy : null;

            if (m_BuyButton != null)
            {
                m_BuyButton.gameObject.SetActive(forSale);
                m_BuyButton.interactable = affordable;
            }
            if (m_BuyLabel != null && forSale)
                m_BuyLabel.text = affordable
                    ? $"Buy  {item.CoinPrice}"
                    : $"Need {item.CoinPrice - balance} more";

            m_Root.SetActive(true);

            if (m_Box != null)
            {
                _popTween?.Kill();
                m_Box.localScale = Vector3.one * 0.85f;
                _popTween = m_Box.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            }
        }

        public void Hide()
        {
            _onBuy = null;
            if (m_Root != null) m_Root.SetActive(false);
        }
    }
}
