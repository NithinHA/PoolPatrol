using System;
using System.Collections.Generic;
using Loadout;
using UnityEngine;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Default <see cref="ILoadoutService"/>. Each of the four cosmetic categories is a
    /// <see cref="Category{T}"/> that owns its list, its selected index, and its PlayerPrefs key.
    /// Selections persist by item <c>Id</c> (robust to list reordering) and default to the first
    /// unlocked entry. Unlock rules are intentionally simple for now (<c>UnlockedByDefault</c>); this
    /// is the single place to later gate on progression or economy.
    /// </summary>
    public class LoadoutService : ILoadoutService
    {
        /// <summary>One cosmetic category: the catalog list plus the player's current pick.</summary>
        private class Category<T> where T : LoadoutItemSO
        {
            private readonly IReadOnlyList<T> _items;
            private readonly string _prefKey;
            public int SelectedIndex { get; private set; }

            public IReadOnlyList<T> Items => _items;
            public T Selected => _items.Count > 0 ? _items[SelectedIndex] : null;

            public Category(List<T> items, string prefKey)
            {
                _items = items ?? new List<T>();
                _prefKey = prefKey;
                SelectedIndex = RestoreIndex();
            }

            public bool IsUnlocked(int index)
                => index >= 0 && index < _items.Count && _items[index].UnlockedByDefault;

            /// <summary>Selects by index if valid and unlocked. Returns whether the selection moved.</summary>
            public bool Select(int index)
            {
                if (index == SelectedIndex || !IsUnlocked(index))
                    return false;

                SelectedIndex = index;
                PlayerPrefs.SetString(_prefKey, _items[index].Id);
                return true;
            }

            private int RestoreIndex()
            {
                string savedId = PlayerPrefs.GetString(_prefKey, string.Empty);
                if (!string.IsNullOrEmpty(savedId))
                {
                    for (int i = 0; i < _items.Count; i++)
                        if (_items[i].Id == savedId && _items[i].UnlockedByDefault)
                            return i;
                }

                // Fall back to the first unlocked entry.
                for (int i = 0; i < _items.Count; i++)
                    if (_items[i].UnlockedByDefault)
                        return i;

                return 0;
            }
        }

        private const string KeyCharacter   = "Loadout_Character";
        private const string KeyFloatie     = "Loadout_Floatie";
        private const string KeyWeapon      = "Loadout_Weapon";
        private const string KeyPoolTexture = "Loadout_PoolTexture";

        private readonly Category<CharacterDefinitionSO> _characters;
        private readonly Category<FloatieDefinitionSO> _floaties;
        private readonly Category<WeaponDefinitionSO> _weapons;
        private readonly Category<PoolTextureDefinitionSO> _poolTextures;

        public event Action OnLoadoutChanged;

        public LoadoutService(LoadoutCatalogSO catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("[LoadoutService] No LoadoutCatalog assigned on Bootstrap — the loadout screen will be empty.");
                catalog = ScriptableObject.CreateInstance<LoadoutCatalogSO>();
            }

            _characters   = new Category<CharacterDefinitionSO>(catalog.Characters, KeyCharacter);
            _floaties     = new Category<FloatieDefinitionSO>(catalog.Floaties, KeyFloatie);
            _weapons      = new Category<WeaponDefinitionSO>(catalog.Weapons, KeyWeapon);
            _poolTextures = new Category<PoolTextureDefinitionSO>(catalog.PoolTextures, KeyPoolTexture);
        }

        public void Start() { }
        public void OnDestroy() { PlayerPrefs.Save(); }

        public IReadOnlyList<CharacterDefinitionSO> Characters => _characters.Items;
        public IReadOnlyList<FloatieDefinitionSO> Floaties => _floaties.Items;
        public IReadOnlyList<WeaponDefinitionSO> Weapons => _weapons.Items;
        public IReadOnlyList<PoolTextureDefinitionSO> PoolTextures => _poolTextures.Items;

        public int SelectedCharacterIndex => _characters.SelectedIndex;
        public int SelectedFloatieIndex => _floaties.SelectedIndex;
        public int SelectedWeaponIndex => _weapons.SelectedIndex;
        public int SelectedPoolTextureIndex => _poolTextures.SelectedIndex;

        public CharacterDefinitionSO SelectedCharacter => _characters.Selected;
        public FloatieDefinitionSO SelectedFloatie => _floaties.Selected;
        public WeaponDefinitionSO SelectedWeapon => _weapons.Selected;
        public PoolTextureDefinitionSO SelectedPoolTexture => _poolTextures.Selected;

        public void SelectCharacter(int index) { if (_characters.Select(index)) Changed(); }
        public void SelectFloatie(int index) { if (_floaties.Select(index)) Changed(); }
        public void SelectWeapon(int index) { if (_weapons.Select(index)) Changed(); }
        public void SelectPoolTexture(int index) { if (_poolTextures.Select(index)) Changed(); }

        public bool IsCharacterUnlocked(int index) => _characters.IsUnlocked(index);
        public bool IsFloatieUnlocked(int index) => _floaties.IsUnlocked(index);
        public bool IsWeaponUnlocked(int index) => _weapons.IsUnlocked(index);
        public bool IsPoolTextureUnlocked(int index) => _poolTextures.IsUnlocked(index);

        private void Changed()
        {
            PlayerPrefs.Save();
            OnLoadoutChanged?.Invoke();
        }
    }
}
