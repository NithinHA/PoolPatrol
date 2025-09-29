using UnityEngine;

namespace Enemy.Movement
{
    public class ChaseStepMovement : EnemyMovement
    {
        [SerializeField] private float m_ForceMagnitude = 2f;
        [Space]
        [SerializeField] private float m_Interval = 1.5f;
        [SerializeField] private float m_StartingInterval = .5f;

        private Transform _target;
        private float _nextMoveTime;

        protected override void Awake()
        {
            base.Awake();
            LockTarget();
            _nextMoveTime = Time.time + m_StartingInterval;
        }

        public override void Tick()
        {
            if (!_target)
                LockTarget();

            if (Time.time >= _nextMoveTime)
            {
                MoveDirection = (_target.position - transform.position).normalized;
                Controller.RigidBody.linearVelocity = Vector2.zero;
                Controller.RigidBody.AddForce(MoveDirection * m_ForceMagnitude);
                _nextMoveTime = Time.time + m_Interval;
                AdjustRotation();
            }
        }

        public override void FixedTick()
        {

        }

        /// <summary>
        /// This function will fetch nearest player from AllPlayersController or similar script.
        /// </summary>
        private void LockTarget()
        {
            _target = GameObject.FindWithTag(Constants.GameConstants.TAG_Player).transform;
        }
    }
}
