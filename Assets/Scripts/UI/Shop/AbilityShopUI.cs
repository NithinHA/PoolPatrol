using System;
using System.Collections.Generic;
using Abilities;
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

        [Header("Buttons")]
        [Tooltip("Optional. Discards the current offers and rolls new ones for gems (doc §20).")]
        [SerializeField] private Button m_RefreshButton;

        [SerializeField] private TMP_Text m_RefreshCostText;

        [Header("Labels (optional)")]
        [Tooltip("Live gem balance while shopping.")]
        [SerializeField] private TMP_Text m_WalletText;

        [Tooltip("Shown instead of the cards when the Goddess has nothing left to offer.")]
        [SerializeField] private GameObject m_NothingLeftMessage;

        /// <summary>Raised whenever a purchase completes, for SFX/feedback hooks.</summary>
        public event Action<AbilityDefinition, int> OnPurchased;

        public bool IsOpen { get; private set; }

        private readonly List<AbilityDefinition> _lastOfferedDefinitions = new();

        private IAbilityService _abilities;
        private IEconomyService _economy;
        private IPauseService _pause;

        private ArenaSpawnConfigSO.GoddessEncounters _settings;
        private Action _onClosed;
        private int _refreshesUsed;
        private bool _initialised;
        private bool _subscribedToEconomy;

        private void Awake() => EnsureInitialised();

        private void OnDestroy()
        {
            if (_economy != null)
                _economy.OnBalanceChanged -= OnBalanceChanged;

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

            ResolveServices();
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
        /// Pauses the game and shows a fresh set of offers.
        /// </summary>
        /// <param name="settings">Pacing/pricing for this arena. Null falls back to sane defaults.</param>
        /// <param name="onClosed">Invoked after the panel hides and the game resumes.</param>
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

            GenerateOffers(excludeCurrent: false);
            RefreshWallet();
            RefreshRefreshButton();
        }

        /// <summary>Exit button: hide, resume, hand back to the encounter director.</summary>
        public void Close()
        {
            if (!IsOpen)
                return;

            CloseInternal();

            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        /// <summary>
        /// Closes without notifying the opener. For teardown paths (scene unload, game over) where
        /// the follow-up behaviour no longer makes sense but the game must not stay frozen.
        /// </summary>
        public void ForceClose()
        {
            if (!IsOpen)
                return;

            _onClosed = null;
            CloseInternal();
        }

        private void CloseInternal()
        {
            IsOpen = false;

            foreach (AbilityOfferCard card in m_Cards)
            {
                if (card != null)
                    card.Clear();
            }

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
            if (_abilities == null || !_abilities.TryPurchase(card.Offer))
                return;

            // Purchase already took effect: gems spent, modifiers applied through RunModifierService,
            // instant effects fired. All that is left is to reflect it in the panel.
            card.MarkSold();
            OnPurchased?.Invoke(card.Offer.Definition, card.Offer.Level);

            RefreshWallet();
            RefreshAffordability();
            RefreshRefreshButton();
        }

        private void Refresh()
        {
            if (!CanRefresh())
                return;

            int cost = CurrentRefreshCost();
            if (cost > 0 && (_economy == null || !_economy.TrySpend(CurrencyType.Gems, cost)))
                return;

            _refreshesUsed++;
            GenerateOffers(excludeCurrent: true);
            RefreshWallet();
            RefreshRefreshButton();
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
            if (_settings == null || _settings.RefreshCost <= 0)
                return false;
            if (_settings.MaxRefreshesPerVisit > 0 && _refreshesUsed >= _settings.MaxRefreshesPerVisit)
                return false;

            int cost = CurrentRefreshCost();
            return _economy != null && _economy.CanAfford(CurrencyType.Gems, cost);
        }
    }
}
