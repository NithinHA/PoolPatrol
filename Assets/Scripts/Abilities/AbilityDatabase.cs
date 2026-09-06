using System.Collections.Generic;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// The complete pool of abilities the Goddess can draw from (doc §17 step 1).
    /// Create one via Assets → Create → PoolPatrol → Ability Database and assign it on the
    /// <c>AbilityServiceInstaller</c> in the Bootstrap scene.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Ability Database", fileName = "AbilityDatabase")]
    public class AbilityDatabase : ScriptableObject
    {
        [SerializeField] private List<AbilityDefinition> m_Abilities = new();

        public IReadOnlyList<AbilityDefinition> Abilities => m_Abilities;

        public void SetAbilities(List<AbilityDefinition> abilities) => m_Abilities = abilities;

        private void OnValidate()
        {
            // Surface duplicate ids early — they would collide in save data and logs.
            HashSet<string> seen = new();
            foreach (AbilityDefinition ability in m_Abilities)
            {
                if (ability == null)
                    continue;
                if (!string.IsNullOrEmpty(ability.Id) && !seen.Add(ability.Id))
                    Debug.LogError($"[AbilityDatabase] Duplicate ability id '{ability.Id}' on {ability.name}.", this);
                if (ability.MaxLevel == 0)
                    Debug.LogError($"[AbilityDatabase] '{ability.name}' has no levels configured.", ability);
            }
        }
    }
}
