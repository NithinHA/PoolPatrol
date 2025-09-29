using UnityEngine;
using Weapon;

namespace Enemy.Attack
{
    public abstract class EnemyAttack : EnemyComponentBase
    {
        [SerializeField] protected EnemyWeaponController m_EnemyWeaponController;

        public abstract void Tick();
    }
}