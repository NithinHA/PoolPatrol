using System;
using Abilities;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shop
{
    /// <summary>
    /// One purchasable slot in the Goddess shop. Presentation only — it never touches the economy
    /// or the ability service, it just renders an <see cref="AbilityOffer"/> and reports clicks
    /// back to <see cref="AbilityShopUI"/>.
    ///
    /// Everything past the button and the name label is optional, so the same component works on a
    /// bare card and on a fully dressed one (doc §26 lists what the player must be able to read:
    /// what it does, what it costs, whether it stacks, and what level they will reach).
    /// </summary>
    public class AbilityOfferCard : MonoBehaviour
    {
        [Header("Required")]
        [Tooltip("The card itself. Clicking it buys the offer.")]
        [SerializeField] private Button m_Button;

        [Tooltip("Ability name, e.g. \"BIGGER BULLETS II\".")]
        [SerializeField] private TMP_Text m_NameText;

        [Header("Optional")]
        [Tooltip("Short effect line, e.g. \"Bullet size +50%\".")]
        [SerializeField] private TMP_Text m_DescriptionText;

        [Tooltip("Gem price.")]
        [SerializeField] private TMP_Text m_PriceText;

        [Tooltip("Level indicator, e.g. \"Level 2 / 3\". Hidden for one-shot abilities.")]
        [SerializeField] private TMP_Text m_LevelText;

        [Tooltip("Ability icon. Hidden when the definition has no sprite assigned yet.")]
        [SerializeField] private Image m_Icon;

        [Tooltip("Tinted by rarity when Use Rarity Tint is on.")]
        [SerializeField] private Image m_Background;

        [Tooltip("Shown once this card has been bought during the current visit.")]
        [SerializeField] private GameObject m_SoldOverlay;

        [Header("Styling")]
        [SerializeField] private bool m_UseRarityTint = true;
        [SerializeField] private Color m_CommonColor   = new(0.85f, 0.88f, 0.92f);
        [SerializeField] private Color m_UncommonColor = new(0.55f, 0.85f, 0.60f);
        [SerializeField] private Color m_RareColor     = new(0.45f, 0.65f, 0.95f);
        [SerializeField] private Color m_EpicColor     = new(0.75f, 0.50f, 0.95f);

        [Tooltip("Price colour when the player cannot afford the offer.")]
        [SerializeField] private Color m_UnaffordableColor = new(0.9f, 0.35f, 0.35f);

        private Color _affordableColor = Color.white;
        private Action<AbilityOfferCard> _onClicked;
        private bool _priceColorCaptured;

        /// <summary>The offer this card is showing. Only meaningful while <see cref="IsBound"/>.</summary>
        public AbilityOffer Offer { get; private set; }

        public bool IsBound { get; private set; }

        /// <summary>True once the player has bought this card during the current shop visit.</summary>
        public bool IsSold { get; private set; }

        private void Awake()
        {
            if (m_Button != null)
                m_Button.onClick.AddListener(HandleClick);

            if (m_PriceText != null && !_priceColorCaptured)
            {
                _affordableColor = m_PriceText.color;
                _priceColorCaptured = true;
            }
        }

        private void OnDestroy()
        {
            if (m_Button != null)
                m_Button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>Shows an offer and starts listening for clicks.</summary>
        public void Bind(AbilityOffer offer, Action<AbilityOfferCard> onClicked)
        {
            Offer = offer;
            IsBound = offer.IsValid;
            IsSold = false;
            _onClicked = onClicked;

            gameObject.SetActive(IsBound);
            if (!IsBound)
                return;

            AbilityDefinition definition = offer.Definition;

            if (m_NameText != null)
                m_NameText.text = offer.DisplayTitle;

            if (m_DescriptionText != null)
                m_DescriptionText.text = offer.Description;

            if (m_PriceText != null)
                m_PriceText.text = offer.Cost.ToString();

            if (m_LevelText != null)
            {
                bool showLevel = definition.IsStackable;
                m_LevelText.gameObject.SetActive(showLevel);
                if (showLevel)
                    m_LevelText.text = $"Level {offer.Level} / {definition.MaxLevel}";
            }

            if (m_Icon != null)
            {
                m_Icon.sprite = definition.Icon;
                m_Icon.enabled = definition.Icon != null;
            }

            if (m_UseRarityTint && m_Background != null)
                m_Background.color = RarityColor(definition.Rarity);

            if (m_SoldOverlay != null)
                m_SoldOverlay.SetActive(false);
        }

        /// <summary>Greys the card out when the player cannot currently afford it.</summary>
        public void SetAffordable(bool affordable)
        {
            if (IsSold)
                return;

            if (m_Button != null)
                m_Button.interactable = affordable;

            if (m_PriceText != null)
                m_PriceText.color = affordable ? _affordableColor : m_UnaffordableColor;
        }

        /// <summary>Locks the card after a successful purchase (doc §15 — it cannot be bought twice).</summary>
        public void MarkSold()
        {
            IsSold = true;

            if (m_Button != null)
                m_Button.interactable = false;

            if (m_SoldOverlay != null)
                m_SoldOverlay.SetActive(true);
            else if (m_PriceText != null)
                m_PriceText.text = "SOLD";
        }

        /// <summary>Hides the card — used when the Goddess has fewer offers than there are slots.</summary>
        public void Clear()
        {
            Offer = default;
            IsBound = false;
            IsSold = false;
            _onClicked = null;
            gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            if (IsBound && !IsSold)
                _onClicked?.Invoke(this);
        }

        private Color RarityColor(AbilityRarity rarity) => rarity switch
        {
            AbilityRarity.Uncommon => m_UncommonColor,
            AbilityRarity.Rare     => m_RareColor,
            AbilityRarity.Epic     => m_EpicColor,
            _                      => m_CommonColor,
        };
    }
}
