using Abilities;
using UnityEngine;

namespace Loadout
{
    /// <summary>
    /// One selectable weapon in the loadout. Maps to an in-game <c>WeaponBase</c> by <see cref="Kind"/>
    /// (no prefab reference, so the loadout stays decoupled from the player prefab). The description
    /// and combo-bonus text are shown on the loadout screen when this weapon is selected.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Loadout/Weapon", fileName = "Weapon")]
    public class WeaponDefinitionSO : LoadoutItemSO
    {
        [Tooltip("Which in-game weapon this selects. Matched against WeaponBase.Kind at game start.")]
        public WeaponKind Kind = WeaponKind.Handgun;

        [TextArea] public string Description;

        [Tooltip("Short line describing the combo bonus this weapon unlocks.")]
        [TextArea] public string ComboBonusDescription;
    }
}
