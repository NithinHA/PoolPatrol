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
            // ImpulseMover.FixedTick() unconditionally writes velocity every step; let an
            // external push (e.g. Damage Revenge) play out before letting it resume.
            if (TickKnockback(Time.fixedDeltaTime))
                return;

            _mover.FixedTick();
        }

        private void OnBounceEvent(Vector2 direction)
        {
            MoveDirection = direction.normalized;
            AdjustRotation();
        }
    }
}
