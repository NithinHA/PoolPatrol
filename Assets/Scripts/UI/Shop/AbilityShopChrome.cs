using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shop
{
    /// <summary>
    /// Everything around the offer bubbles in the Goddess shop: the backdrop fade, the refresh/close
    /// buttons popping in, and lifting the HUD gem wallet above the shop so the player can see their
    /// balance while buying. Presentation only — <see cref="AbilityShopUI"/> decides when to call it.
    ///
    /// Put this on the ShopScreen GameObject next to <see cref="AbilityShopUI"/>. All tweens run on
    /// unscaled time, because the shop freezes the game through <c>Time.timeScale</c>.
    /// </summary>
    public class AbilityShopChrome : MonoBehaviour
    {
        [Header("Backdrop")]
        [Tooltip("Full-screen dimmer behind the bubbles. Give its Image the UIBlur material if you want " +
                 "it blurred — the fade drives the vertex alpha, which that shader respects.")]
        [SerializeField] private Graphic m_Backdrop;

        [Tooltip("Backdrop alpha while the shop is open (0.5 = 50%).")]
        [Range(0f, 1f)]
        [SerializeField] private float m_BackdropAlpha = 0.5f;

        [SerializeField] private float m_BackdropFadeDuration = 0.35f;

        [Header("Buttons")]
        [Tooltip("Refresh, close, etc. They pop in after the backdrop starts and shrink away on close.")]
        [SerializeField] private List<RectTransform> m_Buttons = new();

        [SerializeField] private float m_ButtonsDelay = 0.25f;
        [SerializeField] private float m_ButtonsDuration = 0.3f;
        [SerializeField] private float m_ButtonsStagger = 0.06f;

        [Header("Gem wallet")]
        [Tooltip("The HUD GemWallet's Canvas (Override Sorting on). Raised above the shop while it is open.")]
        [SerializeField] private Canvas m_GemWalletCanvas;

        [SerializeField] private int m_GemWalletOpenSortingOrder = 2;

        /// <summary>Seconds <see cref="Hide"/> takes to finish.</summary>
        public float HideDuration => Mathf.Max(m_BackdropFadeDuration, m_ButtonsDuration * 0.7f);

        private Sequence _sequence;
        private int _walletOriginalOrder;
        private bool _walletRaised;

        private void OnDestroy()
        {
            _sequence?.Kill();
            LowerWallet();
        }

        /// <summary>Fades the backdrop in linearly, pops the buttons in and raises the wallet.</summary>
        public void Show()
        {
            _sequence?.Kill();
            RaiseWallet();
            SetHiddenVisuals();

            _sequence = DOTween.Sequence().SetUpdate(true);

            if (m_Backdrop != null)
                _sequence.Insert(0f, m_Backdrop.DOFade(m_BackdropAlpha, m_BackdropFadeDuration).SetEase(Ease.Linear));

            for (int i = 0; i < m_Buttons.Count; i++)
            {
                if (m_Buttons[i] == null)
                    continue;

                _sequence.Insert(m_ButtonsDelay + i * m_ButtonsStagger,
                    m_Buttons[i].DOScale(1f, m_ButtonsDuration).SetEase(Ease.OutBack));
            }
        }

        /// <summary>Mirror of <see cref="Show"/>. The wallet stays raised until <see cref="ResetHidden"/>.</summary>
        public void Hide()
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true);

            if (m_Backdrop != null)
                _sequence.Insert(0f, m_Backdrop.DOFade(0f, m_BackdropFadeDuration).SetEase(Ease.Linear));

            foreach (RectTransform button in m_Buttons)
            {
                if (button != null)
                    _sequence.Insert(0f, button.DOScale(0f, m_ButtonsDuration * 0.7f).SetEase(Ease.InBack));
            }
        }

        /// <summary>Snaps to the closed state and hands the wallet back its own sorting order.</summary>
        public void ResetHidden()
        {
            _sequence?.Kill();
            _sequence = null;
            SetHiddenVisuals();
            LowerWallet();
        }

        private void SetHiddenVisuals()
        {
            if (m_Backdrop != null)
            {
                Color color = m_Backdrop.color;
                color.a = 0f;
                m_Backdrop.color = color;
            }

            foreach (RectTransform button in m_Buttons)
            {
                if (button != null)
                    button.localScale = Vector3.zero;
            }
        }

        private void RaiseWallet()
        {
            if (m_GemWalletCanvas == null || _walletRaised)
                return;

            _walletOriginalOrder = m_GemWalletCanvas.sortingOrder;
            m_GemWalletCanvas.sortingOrder = m_GemWalletOpenSortingOrder;
            _walletRaised = true;
        }

        private void LowerWallet()
        {
            if (m_GemWalletCanvas == null || !_walletRaised)
                return;

            m_GemWalletCanvas.sortingOrder = _walletOriginalOrder;
            _walletRaised = false;
        }
    }
}
