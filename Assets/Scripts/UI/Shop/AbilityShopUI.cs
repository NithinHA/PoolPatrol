using System;
using System.Collections.Generic;
using Abilities;
using DG.Tweening;
using PTL.Framework;
using PTL.Framework.Services;
using SpawningLogic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shop
{
    /// <summary>
    /// The Pool Goddess shop panel (doc §8, §19-§21). Opened by
    /// <c>GoddessEncounterDirector</c> when the player touches her.
    ///
    /// Responsibilities, in order:
    /// <list type="number">
    ///   <item>Freeze the simulation through <see cref="IPauseService"/> for as long as it is open.</item>
    ///   <item>Ask <see cref="IAbilityService"/> for a set of eligible offers and show them.</item>
    ///   <item>Apply each purchase immediately — instant effects land at once, lasting effects go
    ///         through the run-modifier layer and stay for the rest of the run.</item>
    ///   <item>Optionally re-roll the offers for gems.</item>
    ///   <item>Close on Exit, handing control back to the director (which grants the re-entry
    ///         grace period and retreats the Goddess).</item>
    /// </list>
    ///
    /// Presentation is split out: each card's <see cref="OfferBubble"/> moves it,
    /// <see cref="AbilityShopChrome"/> handles the backdrop, buttons and gem wallet, and
    /// <see cref="GoddessShopkeeper"/> drives the NPC. This class only decides <i>when</i>.
    ///
    /// The cards are fixed scene objects reused across visits and refreshes — a bubble that "leaves"
    /// is deactivated and rebound, never destroyed.
    ///
    /// Attach this to the ShopScreen GameObject itself and point <c>Root</c> at that same object —
    /// it initialises lazily, so it works fine while the object starts out disabled.
    /// </summary>
    public class AbilityShopUI : MonoBehaviour
    {
        [Header("Panel")]
        [Tooltip("Object toggled on/off. Usually this same GameObject.")]
        [SerializeField] private GameObject m_Root;

        [Tooltip("One per offer slot. Extra slots hide themselves when there are fewer offers.")]
        [SerializeField] private List<AbilityOfferCard> m_Cards = new();

        [Tooltip("Bubble sprite for an offer the player can afford.")]
        [SerializeField] private Sprite m_CardBrightSprite;

        [Tooltip("Bubble sprite for an offer the player cannot afford.")]
        [SerializeField] private Sprite m_CardDarkSprite;

        [Header("Buttons")]
        [Tooltip("Optional. Discards the current offers and rolls new ones for gems (doc §20).")]
        [SerializeField] private Button m_RefreshButton;
        [SerializeField] private TMP_Text m_RefreshCostText;

        [Tooltip("Closes the shop. Wired here by code — no Inspector onClick needed.")]
        [SerializeField] private Button m_CloseButton;

        [Header("Labels (optional)")]
        [Tooltip("Live gem balance while shopping.")]
        [SerializeField] private TMP_Text m_WalletText;

        [Tooltip("Shown instead of the cards when the Goddess has nothing left to offer.")]
        [SerializeField] private GameObject m_NothingLeftMessage;

        [Header("Presentation")]
        [Tooltip("Backdrop, buttons and gem wallet. Falls back to one on this GameObject.")]
        [SerializeField] private AbilityShopChrome m_Chrome;

        [Tooltip("The Goddess NPC. Falls back to one under Root.")]
        [SerializeField] private GoddessShopkeeper m_Goddess;

        [Tooltip("Delay before the first bubble floats in.")]
        [SerializeField] private float m_FirstBubbleDelay = 0.15f;

        [Tooltip("Delay between consecutive bubbles floating in.")]
        [SerializeField] private float m_BubbleStagger = 0.08f;

        /// <summary>Raised whenever a purchase completes, for SFX/feedback hooks.</summary>
        public event Action<AbilityDefinition, int> OnPurchased;

        /// <summary>True from <see cref="Open"/> until the close animation has fully finished.</summary>
        public bool IsOpen { get; private set; }

        private readonly List<AbilityDefinition> _lastOfferedDefinitions = new();
        private readonly List<OfferBubble> _bubbles = new();

        private IAbilityService _abilities;
        private IEconomyService _economy;
        private IPauseService _pause;

        private ArenaSpawnConfigSO.GoddessEncounters _settings;
        private Action _onClosed;
        private int _refreshesUsed;
        private bool _initialised;
        private bool _subscribedToEconomy;

        // Transition state. While either flag is set the panel ignores refresh and purchase input.
        private bool _swapping;
        private bool _closing;
        private bool _waitingForGoddess;
        private Tween _pendingStep;

        private void Awake() => EnsureInitialised();

        private void OnDestroy()
        {
            if (_economy != null)
                _economy.OnBalanceChanged -= OnBalanceChanged;

            _pendingStep?.Kill();

            // A scene teardown while the shop is open must not leave timeScale at 0.
            _pause?.Resume(this);
        }

        /// <summary>
        /// Wires buttons. Safe to call repeatedly, and safe to call before the GameObject has ever
        /// been enabled — which is the normal case, since the ShopScreen sits inactive in the scene
        /// until the Goddess is touched.
        /// </summary>
        private void EnsureInitialised()
        {
            if (_initialised)
                return;
            _initialised = true;

            if (m_Root == null)
                m_Root = gameObject;

            if (m_RefreshButton != null)
                m_RefreshButton.onClick.AddListener(Refresh);

            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(Close);

            if (m_Chrome == null)
                m_Chrome = GetComponent<AbilityShopChrome>();

            if (m_Goddess == null)
                m_Goddess = m_Root.GetComponentInChildren<GoddessShopkeeper>(true);

            foreach (AbilityOfferCard card in m_Cards)
            {
                OfferBubble bubble = null;
                if (card != null)
                {
                    if (!card.TryGetComponent(out bubble))
                        bubble = card.gameObject.AddComponent<OfferBubble>();

                    card.SetBubbleSprites(m_CardBrightSprite, m_CardDarkSprite);
                }

                // Kept index-aligned with m_Cards, nulls included.
                _bubbles.Add(bubble);
            }

            ResolveServices();
            if (m_Chrome != null) m_Chrome.ResetHidden();
            m_Root.SetActive(false);
        }

        /// <summary>
        /// Looks the services up late. If this panel is left enabled in the scene its Awake can run
        /// before Bootstrap has registered anything, so the lookup is retried on every open.
        /// </summary>
        private void ResolveServices()
        {
            _abilities ??= ServiceLocator.GetAbilityService();
            _economy   ??= ServiceLocator.GetEconomyService();
            _pause     ??= ServiceLocator.GetPauseService();

            if (_economy == null || _subscribedToEconomy)
                return;

            _economy.OnBalanceChanged += OnBalanceChanged;
            _subscribedToEconomy = true;
        }

#region Open / close

        /// <summary>
        /// Pauses the game, plays the arrival animation and shows a fresh set of offers.
        /// </summary>
        /// <param name="settings">Pacing/pricing for this arena. Null falls back to sane defaults.</param>
        /// <param name="onClosed">Invoked after the panel has finished hiding and the game resumes.</param>
        public void Open(ArenaSpawnConfigSO.GoddessEncounters settings, Action onClosed = null)
        {
            EnsureInitialised();
            ResolveServices();

            if (IsOpen)
                return;

            _settings = settings ?? new ArenaSpawnConfigSO.GoddessEncounters();
            _onClosed = onClosed;
            _refreshesUsed = 0;
            IsOpen = true;

            // Pause first, so nothing can advance between generating offers and showing them.
            _pause?.Pause(this);

            m_Root.SetActive(true);
            if (m_Chrome != null) m_Chrome.Show();
            if (m_Goddess != null) m_Goddess.PlayEnter();

            GenerateOffers(excludeCurrent: false);
            PlayBubblesIn();
            RefreshWallet();
            RefreshRefreshButton();
        }

        /// <summary>
        /// Exit button: shrinks the bubbles, fades the backdrop, lets the Goddess leave, then hides,
        /// resumes and hands back to the encounter director.
        /// </summary>
        public void Close()
        {
            if (!IsOpen || _closing)
                return;

            _closing = true;
            _swapping = false;
            KillPendingStep();
            RefreshRefreshButton();

            float wait = m_Chrome != null ? m_Chrome.HideDuration : 0f;
            for (int i = 0; i < m_Cards.Count; i++)
            {
                if (!IsCardVisible(i))
                    continue;

                _bubbles[i].PlayShrink();
                wait = Mathf.Max(wait, _bubbles[i].ShrinkDuration);
            }

            if (m_Chrome != null) m_Chrome.Hide();

            // Schedule the visual wait before starting the Goddess exit: PlayExit may call back
            // synchronously, and FinishCloseWhenReady must see the visuals still pending.
            _pendingStep = DOVirtual.DelayedCall(wait, () =>
            {
                _pendingStep = null;
                FinishCloseWhenReady();
            }, ignoreTimeScale: true);

            if (m_Goddess != null)
            {
                _waitingForGoddess = true;
                m_Goddess.PlayExit(() =>
                {
                    _waitingForGoddess = false;
                    FinishCloseWhenReady();
                });
            }
        }

        /// <summary>
        /// Closes instantly without notifying the opener. For teardown paths (scene unload, game
        /// over) where the follow-up behaviour no longer makes sense but the game must not stay frozen.
        /// Also cuts short a close animation that is already playing.
        /// </summary>
        public void ForceClose()
        {
            if (!IsOpen)
                return;

            _onClosed = null;
            FinishClose();
        }

        private void FinishCloseWhenReady()
        {
            if (!_closing || _pendingStep != null || _waitingForGoddess)
                return;

            FinishClose();

            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        /// <summary>Puts every piece back to its resting state, ready for the next <see cref="Open"/>.</summary>
        private void FinishClose()
        {
            KillPendingStep();
            if (m_Goddess != null) m_Goddess.StopExit();

            IsOpen = false;
            _closing = false;
            _swapping = false;
            _waitingForGoddess = false;

            for (int i = 0; i < m_Cards.Count; i++)
            {
                if (_bubbles[i] != null)
                    _bubbles[i].ResetToRest();

                if (m_Cards[i] != null)
                    m_Cards[i].Clear();
            }

            if (m_Chrome != null) m_Chrome.ResetHidden();
            m_Root.SetActive(false);
            _pause?.Resume(this);
        }

#endregion

#region Offers

        /// <summary>
        /// Asks the ability service for a new set of eligible offers and binds them to the cards.
        /// </summary>
        /// <param name="excludeCurrent">
        /// True on a refresh, so the discarded pair cannot come straight back (doc §20).
        /// </param>
        private void GenerateOffers(bool excludeCurrent)
        {
            if (_abilities == null)
            {
                Debug.LogError("[AbilityShopUI] No AbilityService registered - the shop has nothing to sell.");
                ShowNothingLeft(true);
                return;
            }

            int slots = Mathf.Min(_settings.OfferCount, m_Cards.Count);
            List<AbilityOffer> offers = _abilities.GenerateOffers(
                slots, excludeCurrent ? _lastOfferedDefinitions : null);

            _lastOfferedDefinitions.Clear();
            foreach (AbilityOffer offer in offers)
                _lastOfferedDefinitions.Add(offer.Definition);

            for (int i = 0; i < m_Cards.Count; i++)
            {
                AbilityOfferCard card = m_Cards[i];
                if (card == null)
                    continue;

                if (i < offers.Count)
                    card.Bind(offers[i], OnCardClicked);
                else
                    card.Clear();
            }

            ShowNothingLeft(offers.Count == 0);
            RefreshAffordability();
        }

        private void OnCardClicked(AbilityOfferCard card)
        {
            if (_swapping || _closing)
                return;

            if (_abilities == null || !_abilities.TryPurchase(card.Offer))
                return;

            // Purchase already took effect: gems spent, modifiers applied through RunModifierService,
            // instant effects fired. All that is left is to reflect it in the panel.
            card.MarkSold();
            OnPurchased?.Invoke(card.Offer.Definition, card.Offer.Level);
            if (m_Goddess != null) m_Goddess.PlayGrant();

            OfferBubble bubble = BubbleFor(card);
            if (bubble != null)
                bubble.PlayPop(card.Clear);
            else
                card.Clear();

            RefreshWallet();
            RefreshAffordability();
            RefreshRefreshButton();
        }

        /// <summary>
        /// Sends the remaining bubbles up off the screen, then rolls and floats in a new set.
        /// </summary>
        private void Refresh()
        {
            if (!CanRefresh())
                return;

            int cost = CurrentRefreshCost();
            if (cost > 0 && (_economy == null || !_economy.TrySpend(CurrencyType.Gems, cost)))
                return;

            _refreshesUsed++;
            _swapping = true;
            if (m_Goddess != null) m_Goddess.PlayRefresh();

            float wait = 0f;
            for (int i = 0; i < m_Cards.Count; i++)
            {
                if (!IsCardVisible(i))
                    continue;

                _bubbles[i].PlayFloatAway();
                wait = Mathf.Max(wait, _bubbles[i].FloatAwayDuration);
            }

            _pendingStep = DOVirtual.DelayedCall(wait, () =>
            {
                _pendingStep = null;
                _swapping = false;

                GenerateOffers(excludeCurrent: true);
                PlayBubblesIn();
                RefreshRefreshButton();
            }, ignoreTimeScale: true);

            RefreshWallet();
            RefreshRefreshButton();
        }

        /// <summary>Floats every bound card in from the centre, one after another.</summary>
        private void PlayBubblesIn()
        {
            float delay = m_FirstBubbleDelay;
            for (int i = 0; i < m_Cards.Count; i++)
            {
                if (!IsCardVisible(i))
                    continue;

                _bubbles[i].PlayIntro(delay);
                delay += m_BubbleStagger;
            }
        }

#endregion

#region Display

        private void OnBalanceChanged(CurrencyType type, int newBalance, int delta)
        {
            if (!IsOpen || type != CurrencyType.Gems)
                return;

            RefreshWallet();
            RefreshAffordability();
            RefreshRefreshButton();
        }

        private void RefreshWallet()
        {
            if (m_WalletText != null && _economy != null)
                m_WalletText.text = _economy.GetBalance(CurrencyType.Gems).ToString();
        }

        /// <summary>
        /// Re-checks every unsold card against the current balance. A purchase can also make a
        /// sibling offer stale (e.g. buying a life when only one was missing), so
        /// <see cref="IAbilityService.CanPurchase"/> is the authority here, not just the price.
        /// </summary>
        private void RefreshAffordability()
        {
            foreach (AbilityOfferCard card in m_Cards)
            {
                if (card == null || !card.IsBound || card.IsSold)
                    continue;

                card.SetAffordable(_abilities != null && _abilities.CanPurchase(card.Offer));
            }
        }

        private void RefreshRefreshButton()
        {
            if (m_RefreshButton == null)
                return;

            bool available = _settings != null && _settings.RefreshCost > 0;
            m_RefreshButton.gameObject.SetActive(available);

            if (!available)
                return;

            int cost = CurrentRefreshCost();
            m_RefreshButton.interactable = CanRefresh();

            if (m_RefreshCostText != null)
                m_RefreshCostText.text = cost.ToString();
        }

        private void ShowNothingLeft(bool show)
        {
            if (m_NothingLeftMessage != null)
                m_NothingLeftMessage.SetActive(show);
        }

#endregion

        /// <summary>Escalating refresh price within a single visit (doc §20: 50 → 75 → 100).</summary>
        private int CurrentRefreshCost()
            => _settings == null ? 0 : _settings.RefreshCost + _settings.RefreshCostIncrement * _refreshesUsed;

        private bool CanRefresh()
        {
            if (_swapping || _closing)
                return false;
            if (_settings == null || _settings.RefreshCost <= 0)
                return false;
            if (_settings.MaxRefreshesPerVisit > 0 && _refreshesUsed >= _settings.MaxRefreshesPerVisit)
                return false;

            int cost = CurrentRefreshCost();
            return _economy != null && _economy.CanAfford(CurrencyType.Gems, cost);
        }

        /// <summary>A bound card whose bubble is still on screen (not popped).</summary>
        private bool IsCardVisible(int index)
        {
            AbilityOfferCard card = m_Cards[index];
            return card != null && _bubbles[index] != null && card.IsBound && card.gameObject.activeSelf;
        }

        private OfferBubble BubbleFor(AbilityOfferCard card)
        {
            int index = m_Cards.IndexOf(card);
            return index >= 0 ? _bubbles[index] : null;
        }

        private void KillPendingStep()
        {
            _pendingStep?.Kill();
            _pendingStep = null;
        }
    }
}
