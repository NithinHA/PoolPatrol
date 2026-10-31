using System;
using System.Collections.Generic;
using Loadout;
using UnityEngine;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Default <see cref="ILoadoutService"/>. Each of the three cosmetic categories is a
    /// <see cref="Category{T}"/> that owns its list, its selected index, and its PlayerPrefs key.
    /// Selections persist by item <c>Id</c> (robust to list reordering) and default to the first
    /// unlocked entry.
    ///
    /// Unlock rules (<see cref="IsUnlocked(LoadoutItemSO)"/>) live here and nowhere else:
    /// Free → always; Coins / IAP → owned; ArenaClear → that arena is completed (or owned).
    /// Ownership persists per item id. Progression and economy are resolved when needed rather than
    /// at construction, since every service is constructed before any is registered.
    /// </summary>
    public class LoadoutService : ILoadoutService
    {
        /// <summary>One cosmetic category: the catalog list plus the player's current pick.</summary>
        private class Category<T> where T : LoadoutItemSO
        {
            private readonly IReadOnlyList<T> _items;
            private readonly string _prefKey;
            private readonly Func<LoadoutItemSO, bool> _isUnlocked;
            public int SelectedIndex { get; private set; }

            public IReadOnlyList<T> Items => _items;
            public T Selected => _items.Count > 0 ? _items[SelectedIndex] : null;

            public Category(List<T> items, string prefKey, Func<LoadoutItemSO, bool> isUnlocked)
            {
                _items = items ?? new List<T>();
                _prefKey = prefKey;
                _isUnlocked = isUnlocked;
            }

            public bool IsUnlocked(int index)
                => index >= 0 && index < _items.Count && _isUnlocked(_items[index]);

            /// <summary>Selects by index if valid and unlocked. Returns whether the selection moved.</summary>
            public bool Select(int index)
            {
                if (index == SelectedIndex || !IsUnlocked(index))
                    return false;

                SelectedIndex = index;
                PlayerPrefs.SetString(_prefKey, _items[index].Id);
                return true;
            }

            /// <summary>
            /// Restores the saved pick. Run from <see cref="LoadoutService.Start"/>, once progression
            /// is registered, so an arena-clear item the player had equipped still counts as unlocked.
            /// </summary>
            public void Restore()
            {
                string savedId = PlayerPrefs.GetString(_prefKey, string.Empty);
                if (!string.IsNullOrEmpty(savedId))
                {
                    for (int i = 0; i < _items.Count; i++)
                        if (_items[i].Id == savedId && IsUnlocked(i))
                        {
                            SelectedIndex = i;
                            return;
                        }
                }

                // Fall back to the first unlocked entry.
                for (int i = 0; i < _items.Count; i++)
                    if (IsUnlocked(i))
                    {
                        SelectedIndex = i;
                        return;
                    }

                SelectedIndex = 0;
            }
        }

        private const string KeyCharacter   = "Loadout_Character";
        private const string KeyFloatie     = "Loadout_Floatie";
        private const string KeyWeapon      = "Loadout_Weapon";
        private const string KeyOwnedPrefix = "Loadout_Owned_";

        private readonly Category<CharacterDefinitionSO> _characters;
        private readonly Category<FloatieDefinitionSO> _floaties;
        private readonly Category<WeaponDefinitionSO> _weapons;

        public event Action OnLoadoutChanged;

        public LoadoutService(LoadoutCatalogSO catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("[LoadoutService] No LoadoutCatalog assigned on Bootstrap — the loadout screen will be empty.");
                catalog = ScriptableObject.CreateInstance<LoadoutCatalogSO>();
            }

            _characters   = new Category<CharacterDefinitionSO>(catalog.Characters, KeyCharacter, IsUnlocked);
            _floaties     = new Category<FloatieDefinitionSO>(catalog.Floaties, KeyFloatie, IsUnlocked);
            _weapons      = new Category<WeaponDefinitionSO>(catalog.Weapons, KeyWeapon, IsUnlocked);
        }

        public void Start()
        {
            _characters.Restore();
            _floaties.Restore();
            _weapons.Restore();
        }

        public void OnDestroy() { PlayerPrefs.Save(); }

        public IReadOnlyList<CharacterDefinitionSO> Characters => _characters.Items;
        public IReadOnlyList<FloatieDefinitionSO> Floaties => _floaties.Items;
        public IReadOnlyList<WeaponDefinitionSO> Weapons => _weapons.Items;

        public int SelectedCharacterIndex => _characters.SelectedIndex;
        public int SelectedFloatieIndex => _floaties.SelectedIndex;
        public int SelectedWeaponIndex => _weapons.SelectedIndex;

        public CharacterDefinitionSO SelectedCharacter => _characters.Selected;
        public FloatieDefinitionSO SelectedFloatie => _floaties.Selected;
        public WeaponDefinitionSO SelectedWeapon => _weapons.Selected;

        public void SelectCharacter(int index) { if (_characters.Select(index)) Changed(); }
        public void SelectFloatie(int index) { if (_floaties.Select(index)) Changed(); }
        public void SelectWeapon(int index) { if (_weapons.Select(index)) Changed(); }

        public bool IsCharacterUnlocked(int index) => _characters.IsUnlocked(index);
        public bool IsFloatieUnlocked(int index) => _floaties.IsUnlocked(index);
        public bool IsWeaponUnlocked(int index) => _weapons.IsUnlocked(index);

        public bool IsUnlocked(LoadoutItemSO item)
        {
            if (item == null)
                return false;

            switch (item.Unlock)
            {
                case UnlockType.Free:
                    return true;

                case UnlockType.ArenaClear:
                    // Owned as well, so an unlock survives the arena list being reordered or trimmed.
                    return IsOwned(item) || (ServiceLocator.GetProgressionService()?.IsArenaCompleted(item.ArenaIndex) ?? false);

                default:    // Coins, IAP
                    return IsOwned(item);
            }
        }

        public PurchaseResult TryPurchase(LoadoutItemSO item)
        {
            if (item == null || item.Unlock != UnlockType.Coins)
                return PurchaseResult.NotForSale;
            if (IsUnlocked(item))
                return PurchaseResult.AlreadyUnlocked;

            IEconomyService economy = ServiceLocator.GetEconomyService();
            if (economy == null)
                return PurchaseResult.Unavailable;
            if (!economy.TrySpend(CurrencyType.Coins, item.CoinPrice))
                return PurchaseResult.InsufficientCoins;

            GrantOwnership(item);
            return PurchaseResult.Success;
        }

        public void GrantOwnership(LoadoutItemSO item)
        {
            if (item == null || string.IsNullOrEmpty(item.Id))
                return;

            PlayerPrefs.SetInt(KeyOwnedPrefix + item.Id, 1);
            Changed();
        }

        private static bool IsOwned(LoadoutItemSO item)
            => !string.IsNullOrEmpty(item.Id) && PlayerPrefs.GetInt(KeyOwnedPrefix + item.Id, 0) == 1;

        private void Changed()
        {
            PlayerPrefs.Save();
            OnLoadoutChanged?.Invoke();
        }
    }
}
