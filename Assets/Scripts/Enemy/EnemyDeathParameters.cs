using UnityEngine;
using Weapon;

namespace Enemy.Death
{
    public struct EnemyDeathParameters
    {
        public GameObject CollidingObject;
        public Vector2 CollisionDirection;

        /// <summary>
        /// The player shot this death is credited to, for scoring. Null for deaths no player shot
        /// caused (enemy friendly fire, environment).
        /// </summary>
        public WeaponAttack Attack;
    }
}
