using System;
using UnityEngine;

namespace Weapon
{
    public class WeaponAttack
    {
        /// <summary>
        /// Fired when the attack successfully hits a valid target. Passes the hit position.
        /// </summary>
        public Action<Vector3> OnHitSuccess;

        /// <summary>
        /// Fired when the attack misses or hits an invalid target (like a wall).
        /// </summary>
        public Action OnHitFail;

        /// <summary>
        /// Fired when the attack is fully resolved and should no longer be tracked (e.g., bullet destroyed or laser finished).
        /// </summary>
        public Action OnAttackComplete;

        /// <summary>
        /// Clears all event subscribers to break reference chains and aid Garbage Collection.
        /// </summary>
        public void Clear()
        {
            OnHitSuccess = null;
            OnHitFail = null;
            OnAttackComplete = null;
        }
    }
}
