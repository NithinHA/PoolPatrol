using Movement;
using UnityEngine;

namespace Enemy.Movement
{
    [RequireComponent(typeof(BounceMovementHandler))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class RandomMovement : EnemyMovement
    {
        [SerializeField] private Transform m_Gfx;
        [SerializeField] private float m_Speed = 3f;

        private BounceMovementHandler _bounceMovementHandler;

        protected override void Awake()
        {
            base.Awake();
            _bounceMovementHandler = GetComponent<BounceMovementHandler>();
            _bounceMovementHandler.AssignParticleEmitter(Controller.WaterRippleParticleEmitter);
        }

        public override void Tick()
        {
            _bounceMovementHandler?.Tick();
            if (_bounceMovementHandler != null && _bounceMovementHandler.DidBounce)
            {
                MoveDirection = _bounceMovementHandler.NewVelocity.normalized;
                transform.position = _bounceMovementHandler.NewPosition;
                AdjustRotation();   // adjustment of rotation is only needed if angle actually changes.
            }
        }

        public override void FixedTick()
        {
            Vector2 velocity = MoveDirection * m_Speed;
            Controller.RigidBody.linearVelocity = velocity;
            _bounceMovementHandler.FixedTick(velocity);
            
        }
    }
}