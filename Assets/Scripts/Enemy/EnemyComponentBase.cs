using UnityEngine;

namespace Enemy
{
    public class EnemyComponentBase : MonoBehaviour
    {
        protected EnemyController Controller;

        protected virtual void Awake()
        {
            Controller = GetComponent<EnemyController>();
        }
    }
}