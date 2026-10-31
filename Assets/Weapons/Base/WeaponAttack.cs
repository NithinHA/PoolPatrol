using System;
using System.Collections.Generic;
using Enemy;
using UnityEngine;

namespace Weapon
{
    public class WeaponAttack
    {
        /// <summary>One enemy this attack killed, captured at the moment of death (the enemy is destroyed right after).</summary>
        public readonly struct Kill
        {
            public readonly int ScoreValue;
            public readonly Vector3 Position;

            public Kill(int scoreValue, Vector3 position)
            {
                ScoreValue = scoreValue;
                Position = position;
            }
        }

        private readonly List<Kill> _kills = new();

        /// <summary>
        /// Fired when the attack successfully hits a valid target. Passes the hit position.
        /// </summary>
        public Action<Vector3> OnHitSuccess;

        /// <summary>
        /// Fired when the attack misses or hits an invalid target (like a wall).
        /// </summary>
        public Action OnHitFail;

        /// <summary>
        /// Fired for every enemy this attack kills — the direct hit and any secondary kills it causes
        /// (combo shockwave, shotgun spread, pierce).
        /// </summary>
        public Action<Kill> OnKill;

        /// <summary>
        /// Fired when the attack is fully resolved and should no longer be tracked (e.g., bullet destroyed or laser finished).
        /// </summary>
        public Action OnAttackComplete;

        /// <summary>
        /// Every kill so far. Instant weapons (shotgun) kill inside <c>FireWeapon</c>, before anyone
        /// can subscribe to <see cref="OnKill"/>, so listeners read this first and then subscribe.
        /// </summary>
        public IReadOnlyList<Kill> Kills => _kills;

        /// <summary>
        /// Attributes a death to this attack. Called by <see cref="EnemyController.Die"/> when the
        /// death parameters carry this attack.
        /// </summary>
        public void RegisterKill(EnemyController enemy)
        {
            Kill kill = new Kill(enemy.ScoreValue, enemy.transform.position);
            _kills.Add(kill);
            OnKill?.Invoke(kill);
        }

        /// <summary>
        /// Clears all event subscribers to break reference chains and aid Garbage Collection.
        /// </summary>
        public void Clear()
        {
            OnHitSuccess = null;
            OnHitFail = null;
            OnKill = null;
            OnAttackComplete = null;
            _kills.Clear();
        }
    }
}
