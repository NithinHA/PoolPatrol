using DG.Tweening;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UI.HUD;
using UnityEngine;

namespace Economy.UI
{
    /// <summary>
    /// In-run coin wallet. There is no permanent coin counter in the HUD: this panel waits above the
    /// gem wallet, outside the screen, and only appears when coins are credited mid-run (a coin pouch):
    ///
    ///   gem wallet fades out → coin wallet slides down into its slot → count rolls up to the new
    ///   balance → short hold → coin wallet slides back up → gem wallet fades back in.
    ///
    /// A second pouch during the sequence just extends it. Grants after the run has ended (the
    /// run-end payout) are ignored — the post-game screen presents those.
    ///
    /// Setup: a sibling of the gem wallet with the same anchors and pivot, excluded from any layout
    /// group (LayoutElement.ignoreLayout). Its shown position is read from <see cref="m_Slot"/> (the
    /// gem wallet), and the slot is also registered as the <see cref="CollectionTargetId.CoinWallet"/>
    /// target, so pouches fly to where the wallet lands rather than where it starts.
    /// </summary>
    public class CoinWalletUI : MonoBehaviour
    {
        [Tooltip("The sliding panel. Defaults to this object, which must be a sibling of the slot.")]
        [SerializeField] private RectTransform m_Panel;
        [SerializeField] private TMP_Text m_CountText;

        [Header("Slot")]
        [Tooltip("The gem wallet: where this panel slides to, and what is hidden while it shows.")]
        [SerializeField] private RectTransform m_Slot;
        [Tooltip("Faded out while the coin wallet is shown. Typically a CanvasGroup on the gem wallet.")]
        [SerializeField] private CanvasGroup m_SlotGroup;

        [Header("Animation")]
        [Tooltip("How far above its slot the panel waits (canvas units) — enough to sit off-screen.")]
        [SerializeField] private float m_HiddenOffset = 450f;
        [SerializeField] private float m_FadeDuration = 0.12f;
        [SerializeField] private float m_SlideDuration = 0.3f;
        [SerializeField] private float m_CountDuration = 0.5f;
        [SerializeField] private float m_HoldDuration = 0.7f;

        private IEconomyService _economy;
        private IRunStatsService _runStats;

        private int _displayed;
        private int _target;
        private Sequence _sequence;

        private void Awake()
        {
            if (m_Panel == null)
                m_Panel = (RectTransform)transform;
        }

        private void OnEnable()
        {
            if (m_Slot != null)
                CollectionTargets.Register(CollectionTargetId.CoinWallet, m_Slot);
        }

        private void OnDisable()
        {
            CollectionTargets.Unregister(CollectionTargetId.CoinWallet);
        }

        private void Start()
        {
            _runStats = ServiceLocator.GetRunStatsService();
            _economy = ServiceLocator.GetEconomyService();
            if (_economy == null)
            {
                Debug.LogWarning("[CoinWalletUI] EconomyService not available.");
                return;
            }

            _displayed = _target = _economy.GetBalance(CurrencyType.Coins);
            SetCount(_displayed);

            // Let the slot's layout group place it first, so the panel parks directly above it.
            Canvas.ForceUpdateCanvases();
            m_Panel.anchoredPosition = HiddenPosition();

            _economy.OnBalanceChanged += OnBalanceChanged;
        }

        private void OnDestroy()
        {
            if (_economy != null)
                _economy.OnBalanceChanged -= OnBalanceChanged;
            _sequence?.Kill();
        }

        private void OnBalanceChanged(CurrencyType type, int newBalance, int delta)
        {
            if (type != CurrencyType.Coins)
                return;

            _target = newBalance;

            if (delta <= 0 || _runStats == null || !_runStats.IsRunActive)
            {
                // Spends and the run-end payout: keep the label honest without presenting.
                if (_sequence == null)
                    SetCount(_displayed = newBalance);
                return;
            }

            Present();
        }

        /// <summary>
        /// (Re)builds the whole sequence from wherever the panel is now, so a grant that lands mid-way
        /// (even while sliding back up) simply returns to the slot and counts on from the shown value.
        /// </summary>
        private void Present()
        {
            _sequence?.Kill();

            Vector2 shown = ShownPosition();
            float remaining = Mathf.Abs(m_Panel.anchoredPosition.y - shown.y) / Mathf.Max(1f, m_HiddenOffset);

            _sequence = DOTween.Sequence();
            if (m_SlotGroup != null)
                _sequence.Append(m_SlotGroup.DOFade(0f, m_FadeDuration));
            _sequence.Append(m_Panel.DOAnchorPos(shown, m_SlideDuration * remaining).SetEase(Ease.OutBack));
            _sequence.Append(DOTween.To(() => _displayed, value => SetCount(_displayed = value), _target, m_CountDuration)
                .SetEase(Ease.OutQuad));
            _sequence.AppendInterval(m_HoldDuration);
            _sequence.Append(m_Panel.DOAnchorPos(HiddenPosition(), m_SlideDuration).SetEase(Ease.InBack));
            if (m_SlotGroup != null)
                _sequence.Append(m_SlotGroup.DOFade(1f, m_FadeDuration));
            _sequence.OnComplete(() => _sequence = null);
        }

        // The slot is a sibling under a layout group, so its position is only final once layout has
        // run — read it each time rather than caching it at Start.
        private Vector2 ShownPosition() => m_Slot != null ? m_Slot.anchoredPosition : Vector2.zero;

        private Vector2 HiddenPosition() => ShownPosition() + Vector2.up * m_HiddenOffset;

        private void SetCount(int value) => HudNumberText.SetGrouped(m_CountText, value);
    }
}
