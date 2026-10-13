using UnityEngine;

namespace Loadout
{
    /// <summary>
    /// One selectable character (head/face) in the loadout. The <see cref="HeadSprite"/> is shown
    /// both on the loadout avatar preview and on the in-game player.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Loadout/Character", fileName = "Character")]
    public class CharacterDefinitionSO : LoadoutItemSO
    {
        [Tooltip("Head/face sprite shown on the loadout avatar and on the in-game player.")]
        public Sprite HeadSprite;
    }
}
