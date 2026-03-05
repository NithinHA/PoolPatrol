using UnityEngine;
using Player;

namespace Weapon
{
    public interface IComboWeapon
    {
        /// <summary>
        /// Called when the player's overall combo level changes.
        /// Useful for changing passive weapon stats (e.g., spread size, bullet count).
        /// </summary>
        void OnComboLevelChanged(PlayerCombo.ComboLevel newLevel);

        /// <summary>
        /// Called dynamically when a specific attack (bullet, etc.) hits a target.
        /// The weapon can use the provided combo level to execute unique behaviors (AoE, chaining, etc.).
        /// </summary>
        void PerformComboHitEffect(Vector2 hitPosition, PlayerCombo.ComboLevel currentLevel);
    }
}
