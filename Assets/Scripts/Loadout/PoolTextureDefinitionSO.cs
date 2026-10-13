using UnityEngine;

namespace Loadout
{
    /// <summary>
    /// One selectable pool texture (the water/tile look of the play area). <see cref="PoolSprite"/>
    /// replaces the Game scene background.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Loadout/Pool Texture", fileName = "PoolTexture")]
    public class PoolTextureDefinitionSO : LoadoutItemSO
    {
        [Tooltip("Pool/water sprite applied to the Game scene background.")]
        public Sprite PoolSprite;
    }
}
