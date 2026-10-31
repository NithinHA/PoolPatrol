using System;
using System.Collections.Generic;
using Loadout;

namespace PTL.Framework.Services
{
    /// <summary>Outcome of <see cref="ILoadoutService.TryPurchase"/>.</summary>
    public enum PurchaseResult
    {
        Success,
        AlreadyUnlocked,
        /// <summary>Not a coin item (free, arena-clear or IAP).</summary>
        NotForSale,
        InsufficientCoins,
        /// <summary>The economy service is missing.</summary>
        Unavailable,
    }

    /// <summary>
    /// Holds the player's cosmetic loadout (character, floatie, weapon), persists the
    /// selection across sessions, and decides which entries are unlocked: free items, items bought
    /// with Coins, items earned by clearing an arena, and (later) IAP items. Registered as a
    /// persistent service in <see cref="Bootstrap"/>; the Loadout screen reads/writes it and the Game
    /// scene's LoadoutApplier reads it to dress the player.
    /// </summary>
    public interface ILoadoutService : IService
    {
        /// <summary>Fired whenever any selection or ownership changes.</summary>
        event Action OnLoadoutChanged;

        IReadOnlyList<CharacterDefinitionSO> Characters { get; }
        IReadOnlyList<FloatieDefinitionSO> Floaties { get; }
        IReadOnlyList<WeaponDefinitionSO> Weapons { get; }

        int SelectedCharacterIndex { get; }
        int SelectedFloatieIndex { get; }
        int SelectedWeaponIndex { get; }

        CharacterDefinitionSO SelectedCharacter { get; }
        FloatieDefinitionSO SelectedFloatie { get; }
        WeaponDefinitionSO SelectedWeapon { get; }

        void SelectCharacter(int index);
        void SelectFloatie(int index);
        void SelectWeapon(int index);

        bool IsCharacterUnlocked(int index);
        bool IsFloatieUnlocked(int index);
        bool IsWeaponUnlocked(int index);

        /// <summary>True if the player can equip <paramref name="item"/> right now, whatever its unlock type.</summary>
        bool IsUnlocked(LoadoutItemSO item);

        /// <summary>
        /// Buys a <see cref="UnlockType.Coins"/> item: spends its price and records ownership.
        /// Fires <see cref="OnLoadoutChanged"/> on success. Does not select it.
        /// </summary>
        PurchaseResult TryPurchase(LoadoutItemSO item);

        /// <summary>
        /// Records ownership without charging — the hook an IAP fulfilment or purchase restore calls.
        /// Fires <see cref="OnLoadoutChanged"/>.
        /// </summary>
        void GrantOwnership(LoadoutItemSO item);
    }
}
