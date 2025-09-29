using Movement;
using UnityEngine;

namespace Enemy.Movement
{
    [RequireComponent(typeof(ImpulseMover))]
    public class EnemyImpulseMover : EnemyMovement
    {
        private ImpulseMover _mover;

        protected override void Awake()
        {
            base.Awake();
            _mover = GetComponent<ImpulseMover>();
            _mover.AssignParticleEmitter(Controller.WaterRippleParticleEmitter);
            _mover.MoveDirection = MoveDirection;
            _mover.OnBounce += OnBounceEvent;
        }

        public override void Tick()
        {
            _mover.Tick();
            this.MoveDirection = _mover.MoveDirection;
        }

        public override void FixedTick()
        {
            _mover.FixedTick();
        }

        private void OnBounceEvent(Vector2 direction)
        {
            MoveDirection = direction.normalized;
            AdjustRotation();
        }
    }
}
