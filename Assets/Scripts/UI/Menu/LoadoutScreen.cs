using System;
using Loadout;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Third menu screen: character / floatie / weapon selectors feeding a live avatar preview, plus
    /// the selected weapon's description and combo-bonus text, the coin balance, and a purchase panel
    /// for locked items. Reports Play / Back up to the navigator; all selections are committed to the
    /// <see cref="ILoadoutService"/> as they change.
    /// </summary>
    public class LoadoutScreen : MonoBehaviour
    {
        [Header("Selectors")]
        [SerializeField] private LoadoutSelectorUI m_CharacterSelector;
        [SerializeField] private LoadoutSelectorUI m_FloatieSelector;
        [SerializeField] private LoadoutSelectorUI m_WeaponSelector;

        [Header("Preview")]
        [SerializeField] private LoadoutAvatarPreview m_Avatar;

        [Header("Weapon info")]
        [SerializeField] private TMP_Text m_WeaponNameText;
        [SerializeField] private Image m_WeaponIcon;
        [SerializeField] private TMP_Text m_WeaponDescriptionText;
        [SerializeField] private TMP_Text m_ComboTitleText;
        [SerializeField] private TMP_Text m_ComboInfoText;

        [Header("Navigation")]
        [SerializeField] private Button m_PlayButton;
        [SerializeField] private Button m_BackButton;

        [Header("Unlocks")]
        [Tooltip("The player's coin balance.")]
        [SerializeField] private TMP_Text m_CoinBalanceText;
        [Tooltip("Opens when a locked item is tapped: price / unlock reason and a Buy button.")]
        [SerializeField] private LoadoutPurchasePanel m_PurchasePanel;

        private ILoadoutService _loadout;
        private IEconomyService _economy;
        private Action _onPlay;
        private Action _onBack;
        private bool _wired;

        public void Bind(Action onPlay, Action onBack)
        {
            _onPlay = onPlay;
            _onBack = onBack;

            if (m_PlayButton != null)
            {
                m_PlayButton.onClick.RemoveAllListeners();
                m_PlayButton.onClick.AddListener(() => _onPlay?.Invoke());
            }
            if (m_BackButton != null)
            {
                m_BackButton.onClick.RemoveAllListeners();
                m_BackButton.onClick.AddListener(() => _onBack?.Invoke());
            }
        }

        /// <summary>Populates every selector from the service and syncs the avatar + weapon text.</summary>
        public void Show()
        {
            _loadout = ServiceLocator.GetLoadoutService();
            if (_loadout == null) return;

            if (_economy == null)
            {
                _economy = ServiceLocator.GetEconomyService();
                if (_economy != null) _economy.OnBalanceChanged += OnBalanceChanged;
            }
            RefreshBalance();
            if (m_PurchasePanel != null) m_PurchasePanel.Hide();

            PopulateSelectors();

            if (m_Avatar != null)
            {
                m_Avatar.SetCharacter(_loadout.SelectedCharacter?.HeadSprite);
                m_Avatar.SetFloatie(_loadout.SelectedFloatie?.BodySprite);
                m_Avatar.SetWeapon(_loadout.SelectedWeapon?.Icon);
            }
            UpdateWeaponInfo(_loadout.SelectedWeapon);
        }

        private void PopulateSelectors()
        {
            if (m_CharacterSelector != null)
                m_CharacterSelector.Populate(_loadout.Characters, _loadout.IsCharacterUnlocked, _loadout.SelectedCharacterIndex,
                    i => LockedTag(_loadout.Characters[i]));
            if (m_FloatieSelector != null)
                m_FloatieSelector.Populate(_loadout.Floaties, _loadout.IsFloatieUnlocked, _loadout.SelectedFloatieIndex,
                    i => LockedTag(_loadout.Floaties[i]));
            if (m_WeaponSelector != null)
                m_WeaponSelector.Populate(_loadout.Weapons, _loadout.IsWeaponUnlocked, _loadout.SelectedWeaponIndex,
                    i => LockedTag(_loadout.Weapons[i]));

            // Subscribe once; selectors live for the lifetime of the screen.
            if (_wired) return;
            _wired = true;

            if (m_CharacterSelector != null) m_CharacterSelector.OnChanged += OnCharacterChanged;
            if (m_FloatieSelector != null) m_FloatieSelector.OnChanged += OnFloatieChanged;
            if (m_WeaponSelector != null) m_WeaponSelector.OnChanged += OnWeaponChanged;

            if (m_CharacterSelector != null) m_CharacterSelector.OnLockedClicked += OnLockedCharacter;
            if (m_FloatieSelector != null) m_FloatieSelector.OnLockedClicked += OnLockedFloatie;
            if (m_WeaponSelector != null) m_WeaponSelector.OnLockedClicked += OnLockedWeapon;
        }

#region Unlocks

        private void OnLockedCharacter(int index) => OfferUnlock(m_CharacterSelector, _loadout.Characters[index], index);
        private void OnLockedFloatie(int index) => OfferUnlock(m_FloatieSelector, _loadout.Floaties[index], index);
        private void OnLockedWeapon(int index) => OfferUnlock(m_WeaponSelector, _loadout.Weapons[index], index);

        private void OfferUnlock(LoadoutSelectorUI selector, LoadoutItemSO item, int index)
        {
            if (m_PurchasePanel == null || item == null) return;

            m_PurchasePanel.Show(item, UnlockReason(item), Balance, () => Buy(selector, item, index));
        }

        private void Buy(LoadoutSelectorUI selector, LoadoutItemSO item, int index)
        {
            PurchaseResult result = _loadout.TryPurchase(item);
            if (result == PurchaseResult.Success || result == PurchaseResult.AlreadyUnlocked)
            {
                m_PurchasePanel.Hide();
                // Equip what was just bought: the selector fires OnChanged, which selects it in the service.
                selector.RefreshAndSelect(index);
                return;
            }

            // Balance moved underneath the panel (or the purchase failed): re-show with fresh numbers.
            Debug.LogWarning($"[LoadoutScreen] Purchase of '{item.Id}' failed: {result}");
            OfferUnlock(selector, item, index);
        }

        /// <summary>The short label on a locked thumbnail.</summary>
        private static string LockedTag(LoadoutItemSO item) => item.Unlock switch
        {
            UnlockType.Coins      => item.CoinPrice.ToString(),
            UnlockType.ArenaClear => $"Arena {item.ArenaIndex + 1}",
            UnlockType.IAP        => "Store",
            _                     => null,
        };

        private string UnlockReason(LoadoutItemSO item)
        {
            switch (item.Unlock)
            {
                case UnlockType.Coins:
                    return $"Unlock for {item.CoinPrice} coins.\nYou have {Balance}.";

                case UnlockType.ArenaClear:
                    return $"Clear every level of {ArenaName(item.ArenaIndex)} to unlock.";

                case UnlockType.IAP:
                    return "Coming soon to the store.";

                default:
                    return string.Empty;
            }
        }

        private static string ArenaName(int arenaIndex)
        {
            IProgression progression = ServiceLocator.GetProgressionService();
            if (progression != null && arenaIndex >= 0 && arenaIndex < progression.ArenaCount)
            {
                string arenaName = progression.GetArenaDefinition(arenaIndex).ArenaName;
                if (!string.IsNullOrEmpty(arenaName)) return arenaName;
            }
            return $"Arena {arenaIndex + 1}";
        }

        private int Balance => _economy?.GetBalance(CurrencyType.Coins) ?? 0;

        private void OnBalanceChanged(CurrencyType type, int newBalance, int delta)
        {
            if (type == CurrencyType.Coins) RefreshBalance();
        }

        private void RefreshBalance()
        {
            if (m_CoinBalanceText != null) m_CoinBalanceText.text = Balance.ToString();
        }

#endregion

        private void OnCharacterChanged(int index)
        {
            _loadout.SelectCharacter(index);
            if (m_Avatar != null)
            {
                m_Avatar.SetCharacter(_loadout.SelectedCharacter?.HeadSprite);
                m_Avatar.PlayChangeReaction();
            }
        }

        private void OnFloatieChanged(int index)
        {
            _loadout.SelectFloatie(index);
            if (m_Avatar != null)
            {
                m_Avatar.SetFloatie(_loadout.SelectedFloatie?.BodySprite);
                m_Avatar.PlayChangeReaction();
            }
        }

        private void OnWeaponChanged(int index)
        {
            _loadout.SelectWeapon(index);
            if (m_Avatar != null)
            {
                m_Avatar.SetWeapon(_loadout.SelectedWeapon?.Icon);
                m_Avatar.PlayChangeReaction();
            }
            UpdateWeaponInfo(_loadout.SelectedWeapon);
        }

        private void UpdateWeaponInfo(WeaponDefinitionSO weapon)
        {
            if (weapon == null) return;
            if (m_WeaponNameText != null) m_WeaponNameText.text = weapon.DisplayName;
            if (m_WeaponIcon != null)
            {
                m_WeaponIcon.sprite = weapon.Icon;
                m_WeaponIcon.enabled = weapon.Icon != null;
            }
            if (m_WeaponDescriptionText != null) m_WeaponDescriptionText.text = weapon.Description;
            if (m_ComboInfoText != null) m_ComboInfoText.text = weapon.ComboBonusDescription;
            if (m_ComboTitleText != null)
                m_ComboTitleText.gameObject.SetActive(!string.IsNullOrEmpty(weapon.ComboBonusDescription));
        }

        private void OnDestroy()
        {
            if (m_CharacterSelector != null) m_CharacterSelector.OnChanged -= OnCharacterChanged;
            if (m_FloatieSelector != null) m_FloatieSelector.OnChanged -= OnFloatieChanged;
            if (m_WeaponSelector != null) m_WeaponSelector.OnChanged -= OnWeaponChanged;

            if (m_CharacterSelector != null) m_CharacterSelector.OnLockedClicked -= OnLockedCharacter;
            if (m_FloatieSelector != null) m_FloatieSelector.OnLockedClicked -= OnLockedFloatie;
            if (m_WeaponSelector != null) m_WeaponSelector.OnLockedClicked -= OnLockedWeapon;

            if (_economy != null) _economy.OnBalanceChanged -= OnBalanceChanged;
        }
    }
}
