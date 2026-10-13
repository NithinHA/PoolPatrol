using System;
using System.Collections.Generic;
using Loadout;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Holds the player's cosmetic loadout (character, floatie, weapon, pool texture), persists the
    /// selection across sessions, and answers which entries are unlocked. Registered as a persistent
    /// service in <see cref="Bootstrap"/>; the Loadout screen reads/writes it and the Game scene's
    /// LoadoutApplier reads it to dress the player.
    /// </summary>
    public interface ILoadoutService : IService
    {
        /// <summary>Fired whenever any selection changes.</summary>
        event Action OnLoadoutChanged;

        IReadOnlyList<CharacterDefinitionSO> Characters { get; }
        IReadOnlyList<FloatieDefinitionSO> Floaties { get; }
        IReadOnlyList<WeaponDefinitionSO> Weapons { get; }
        IReadOnlyList<PoolTextureDefinitionSO> PoolTextures { get; }

        int SelectedCharacterIndex { get; }
        int SelectedFloatieIndex { get; }
        int SelectedWeaponIndex { get; }
        int SelectedPoolTextureIndex { get; }

        CharacterDefinitionSO SelectedCharacter { get; }
        FloatieDefinitionSO SelectedFloatie { get; }
        WeaponDefinitionSO SelectedWeapon { get; }
        PoolTextureDefinitionSO SelectedPoolTexture { get; }

        void SelectCharacter(int index);
        void SelectFloatie(int index);
        void SelectWeapon(int index);
        void SelectPoolTexture(int index);

        bool IsCharacterUnlocked(int index);
        bool IsFloatieUnlocked(int index);
        bool IsWeaponUnlocked(int index);
        bool IsPoolTextureUnlocked(int index);
    }
}
