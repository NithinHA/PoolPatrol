using System.Collections.Generic;
using UnityEngine;

namespace Monetization
{
    /// <summary>
    /// The Bundles tab's content: every coin pack and item bundle, in display order. Assigned on
    /// <c>Bootstrap</c> and handed to the <c>ShopService</c>. The Characters / Floaties / Weapons tabs
    /// read the <see cref="Loadout.LoadoutCatalogSO"/> instead, so an item only exists in one place.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Shop/Catalog", fileName = "ShopCatalog")]
    public class ShopCatalogSO : ScriptableObject
    {
        public List<CoinPackSO> CoinPacks = new();
        public List<ItemBundleSO> Bundles = new();
    }
}
