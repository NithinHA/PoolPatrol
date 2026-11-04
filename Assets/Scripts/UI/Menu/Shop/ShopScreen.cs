using System;
using DG.Tweening;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// The main-menu Shop: tabs, coin balance, and a short status message for purchase results. Panels
    /// and cards only read state and call <see cref="IShopService"/>; this screen listens for anything
    /// that changes what they show and refreshes the open tab. Sliding it in and out is the
    /// <see cref="MenuNavigator"/>'s job, and this is unrelated to the in-run Goddess shop in UI.Shop.
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        [Serializable]
        private struct Tab
        {
            public ShopTabButton Button;
            public ShopPanel Panel;
        }

        [Header("Tabs (in display order)")]
        [SerializeField] private Tab[] m_Tabs;

        [Header("Wallet")]
        [SerializeField] private TMP_Text m_CoinBalanceText;
        [SerializeField] private RectTransform m_Wallet;

        [Header("Buttons")]
        [SerializeField] private Button m_CloseButton;
        [Tooltip("Apple requires a way to restore one-time purchases. Shown on iOS and in the editor.")]
        [SerializeField] private Button m_RestoreButton;

        [Header("Status message")]
        [SerializeField] private CanvasGroup m_Status;
        [SerializeField] private TMP_Text m_StatusText;
        [SerializeField] private float m_StatusSeconds = 2f;

        private IShopService _shop;
        private ILoadoutService _loadout;
        private IEconomyService _economy;
        private Action _onClose;
        private int _selectedTab;
        private Tween _statusTween;
        private Tween _walletTween;

        public void Bind(Action onClose)
        {
            _onClose = onClose;

            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.RemoveAllListeners();
                m_CloseButton.onClick.AddListener(() => _onClose?.Invoke());
            }

            if (m_RestoreButton != null)
            {
                m_RestoreButton.gameObject.SetActive(Application.isEditor || Application.platform == RuntimePlatform.IPhonePlayer);
                m_RestoreButton.onClick.RemoveAllListeners();
                m_RestoreButton.onClick.AddListener(Restore);
            }

            for (int i = 0; i < m_Tabs.Length; i++)
            {
                int captured = i;
                m_Tabs[i].Button.Bind(() => SelectTab(captured));
            }
        }

        /// <summary>Called by the navigator as the screen slides in. Always opens on the first tab.</summary>
        public void Show()
        {
            if (m_Status != null) m_Status.alpha = 0f;
            RefreshBalance();
            SelectTab(0);
        }

        private void OnEnable()
        {
            _shop = ServiceLocator.GetShopService();
            _loadout = ServiceLocator.GetLoadoutService();
            _economy = ServiceLocator.GetEconomyService();

            if (_shop != null)
            {
                _shop.OnStoreStateChanged += RefreshOpenTab;
                _shop.OnPurchaseResolved += OnPurchaseResolved;
            }
            if (_loadout != null) _loadout.OnLoadoutChanged += RefreshOpenTab;
            if (_economy != null) _economy.OnBalanceChanged += OnBalanceChanged;
        }

        private void OnDisable()
        {
            if (_shop != null)
            {
                _shop.OnStoreStateChanged -= RefreshOpenTab;
                _shop.OnPurchaseResolved -= OnPurchaseResolved;
            }
            if (_loadout != null) _loadout.OnLoadoutChanged -= RefreshOpenTab;
            if (_economy != null) _economy.OnBalanceChanged -= OnBalanceChanged;

            _statusTween?.Kill();
            _walletTween?.Kill(true);
        }

        private void SelectTab(int index)
        {
            _selectedTab = index;
            for (int i = 0; i < m_Tabs.Length; i++)
            {
                m_Tabs[i].Button.SetSelected(i == index);
                m_Tabs[i].Panel.gameObject.SetActive(i == index);
            }

            m_Tabs[index].Panel.Refresh();
            m_Tabs[index].Panel.ResetView();
        }

        private void RefreshOpenTab()
        {
            if (m_Tabs != null && _selectedTab < m_Tabs.Length)
                m_Tabs[_selectedTab].Panel.Refresh();
        }

        private void OnPurchaseResolved(ShopResult result)
        {
            switch (result.Outcome)
            {
                case ShopOutcome.Success:
                    ShowStatus($"{result.DisplayName} added!");
                    break;

                case ShopOutcome.InsufficientCoins:
                    ShowStatus($"You need {result.Shortfall:N0} more coins");
                    PunchWallet();
                    break;

                case ShopOutcome.Deferred:
                    ShowStatus("Waiting for approval");
                    break;

                case ShopOutcome.Unavailable:
                    ShowStatus("The store isn't available right now");
                    break;

                case ShopOutcome.Failed:
                    ShowStatus("The purchase didn't go through");
                    break;

                // Cancelled: the player chose that, nothing to say.
            }
        }

        private void Restore()
        {
            _shop?.RestorePurchases(success =>
            {
                if (this != null && isActiveAndEnabled)
                    ShowStatus(success ? "Purchases restored" : "Couldn't restore purchases");
            });
        }

        private void ShowStatus(string message)
        {
            if (m_Status == null || m_StatusText == null) return;

            m_StatusText.text = message;
            _statusTween?.Kill();
            m_Status.alpha = 1f;
            _statusTween = m_Status.DOFade(0f, 0.3f).SetDelay(m_StatusSeconds).SetUpdate(true);
        }

#region Wallet

        private void OnBalanceChanged(CurrencyType type, int newBalance, int delta)
        {
            if (type != CurrencyType.Coins) return;
            RefreshBalance();
            PunchWallet();
        }

        private void RefreshBalance()
        {
            if (m_CoinBalanceText != null)
                m_CoinBalanceText.text = (_economy?.GetBalance(CurrencyType.Coins) ?? 0).ToString("N0");
        }

        private void PunchWallet()
        {
            if (m_Wallet == null) return;
            _walletTween?.Kill(true);
            _walletTween = m_Wallet.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.5f).SetUpdate(true);
        }

#endregion
    }
}
