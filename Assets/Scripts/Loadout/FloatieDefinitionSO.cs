using UnityEngine;

namespace Loadout
{
    /// <summary>
    /// One selectable floatie (the inflatable the player rides). <see cref="BodySprite"/> drives the
    /// in-game floatie graphic and the loadout avatar; <see cref="ReflectionSprite"/> is the optional
    /// mirrored copy used by the water reflection (falls back to <see cref="BodySprite"/> if unset).
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Loadout/Floatie", fileName = "Floatie")]
    public class FloatieDefinitionSO : LoadoutItemSO
    {
        [Tooltip("Main floatie sprite shown in-game and on the loadout avatar.")]
        public Sprite BodySprite;

        [Tooltip("Optional reflection sprite. Falls back to BodySprite when empty.")]
        public Sprite ReflectionSprite;
    }
}
