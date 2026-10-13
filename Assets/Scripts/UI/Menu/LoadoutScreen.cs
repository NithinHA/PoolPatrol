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
    /// Third menu screen: character / floatie / weapon / pool-texture selectors feeding a live avatar
    /// preview, plus the selected weapon's description and combo-bonus text. Reports Play / Back up to
    /// the navigator; all selections are committed to the <see cref="ILoadoutService"/> as they change.
    /// </summary>
    public class LoadoutScreen : MonoBehaviour
    {
        [Header("Selectors")]
        [SerializeField] private LoadoutSelectorUI m_CharacterSelector;
        [SerializeField] private LoadoutSelectorUI m_FloatieSelector;
        [SerializeField] private LoadoutSelectorUI m_WeaponSelector;
        [SerializeField] private LoadoutSelectorUI m_PoolTextureSelector;

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

        private ILoadoutService _loadout;
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
                m_CharacterSelector.Populate(_loadout.Characters, _loadout.IsCharacterUnlocked, _loadout.SelectedCharacterIndex);
            if (m_FloatieSelector != null)
                m_FloatieSelector.Populate(_loadout.Floaties, _loadout.IsFloatieUnlocked, _loadout.SelectedFloatieIndex);
            if (m_WeaponSelector != null)
                m_WeaponSelector.Populate(_loadout.Weapons, _loadout.IsWeaponUnlocked, _loadout.SelectedWeaponIndex);
            if (m_PoolTextureSelector != null)
                m_PoolTextureSelector.Populate(_loadout.PoolTextures, _loadout.IsPoolTextureUnlocked, _loadout.SelectedPoolTextureIndex);

            // Subscribe once; selectors live for the lifetime of the screen.
            if (_wired) return;
            _wired = true;

            if (m_CharacterSelector != null) m_CharacterSelector.OnChanged += OnCharacterChanged;
            if (m_FloatieSelector != null) m_FloatieSelector.OnChanged += OnFloatieChanged;
            if (m_WeaponSelector != null) m_WeaponSelector.OnChanged += OnWeaponChanged;
            if (m_PoolTextureSelector != null) m_PoolTextureSelector.OnChanged += OnPoolTextureChanged;
        }

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

        private void OnPoolTextureChanged(int index)
        {
            _loadout.SelectPoolTexture(index);
            // Ripple only — the pool texture doesn't belong to the avatar's own body, so it shouldn't
            // squash/stretch in reaction to it.
            if (m_Avatar != null) m_Avatar.PlayRippleOnly();
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
            if (m_PoolTextureSelector != null) m_PoolTextureSelector.OnChanged -= OnPoolTextureChanged;
        }
    }
}
