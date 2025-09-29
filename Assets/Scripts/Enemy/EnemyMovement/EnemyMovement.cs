using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemy.Movement
{
    public abstract class EnemyMovement : EnemyComponentBase
    {
        [SerializeField] private float m_RotationSpeed = .6f;
        [Space]

        protected Vector2 MoveDirection;
        private Tween _rotationTween;

#region Unity callbacks
        
        protected override void Awake()
        {
            base.Awake();
            SetRandomDirection();
            AdjustRotation();
        }

        protected virtual void OnDestroy()
        {
            if (_rotationTween != null && _rotationTween.IsActive() && _rotationTween.IsPlaying())
                _rotationTween.Kill();
        }

#endregion

        public abstract void Tick();
        public abstract void FixedTick();

        public void PushBack(float pushBackForce, Vector2 direction)
        {
            Controller.RigidBody.AddForce(direction * pushBackForce, ForceMode2D.Impulse);
        }

        protected virtual void SetRandomDirection()
        {
            do
            {
                MoveDirection = Random.insideUnitCircle.normalized;
            } while (MoveDirection == Vector2.zero);
        }
        
        protected void AdjustRotation()
        {
            float targetAngle = Mathf.Atan2(MoveDirection.y, MoveDirection.x) * Mathf.Rad2Deg - 90f;

            // Kill existing tween if it's running
            if (_rotationTween != null && _rotationTween.IsActive() && _rotationTween.IsPlaying())
                _rotationTween.Kill();

            // Create and cache the new tween
            _rotationTween = transform.DORotate(new Vector3(0, 0, targetAngle), m_RotationSpeed)
                .SetEase(Ease.OutFlash);
        }
    }
}