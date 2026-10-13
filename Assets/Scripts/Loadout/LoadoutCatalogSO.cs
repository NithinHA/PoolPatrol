using System.Collections.Generic;
using UnityEngine;

namespace Loadout
{
    /// <summary>
    /// The master list of every loadout cosmetic the game ships with. Assigned on <c>Bootstrap</c> and
    /// handed to the <c>LoadoutService</c>, mirroring how the AbilityDatabase and arena list are fed in.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Loadout/Catalog", fileName = "LoadoutCatalog")]
    public class LoadoutCatalogSO : ScriptableObject
    {
        public List<CharacterDefinitionSO> Characters = new();
        public List<FloatieDefinitionSO> Floaties = new();
        public List<WeaponDefinitionSO> Weapons = new();
        public List<PoolTextureDefinitionSO> PoolTextures = new();
    }
}
