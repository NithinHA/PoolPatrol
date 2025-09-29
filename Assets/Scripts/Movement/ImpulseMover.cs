using System;
using UnityEngine;

namespace Movement
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ImpulseMover : MonoBehaviour
    {
        [Header("Movement settings")] public float m_ImpulseStrength = 12f;
        public float m_TargetSpeed = 3f;
        public float m_SmoothingTime = 0.2f;

        private ParticleEmitter _rippleParticleEmitter = null;     // this reference must be set by the controller (player/enemy)
        private Rigidbody2D _rb;
        private Vector2 _moveDirection;
        private float _smoothingTimer;
        private bool _isDecaying;
        private Vector2 _lastVelocity;

        public bool IsDecaying => _isDecaying;

        public Action<Vector2> OnBounce;

        public Vector2 MoveDirection
        {
            get => _moveDirection;
            set => _moveDirection = value;
        }

        public void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.linearDamping = 0;
        }

#region Updates

        public void Tick()
        {
            if (!_isDecaying) // keep velocity steady after decay
            {
                _rb.linearVelocity = _moveDirection * m_TargetSpeed;
            }

            CheckScreenBounce();
        }

        public void FixedTick()
        {
            _lastVelocity = _rb.linearVelocity;

            if (_isDecaying)
            {
                _smoothingTimer += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(_smoothingTimer / m_SmoothingTime);
                float currentSpeed = Mathf.Lerp(m_ImpulseStrength, m_TargetSpeed, t);
                _rb.linearVelocity = _moveDirection * currentSpeed;

                if (t >= 1f)
                    _isDecaying = false;
            }
        }

        public void AssignParticleEmitter(ParticleEmitter emitter)
        {
            _rippleParticleEmitter = emitter;
        }

#endregion

        public void ApplyImpulse(Vector2 direction)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(direction * m_ImpulseStrength, ForceMode2D.Impulse);
            _moveDirection = direction.normalized;
            _smoothingTimer = 0f;
            _isDecaying = true;
            _rippleParticleEmitter?.EmitParticles();
        }

        /// <summary>
        /// Directly sets _moveDirection.
        /// No use found so far.
        /// </summary>
        public void SetMoveDirection(Vector2 dir)
        {
            _moveDirection = dir.normalized;
        }

        private void CheckScreenBounce()
        {
            Vector2 pos = transform.position;
            Vector2 vel = _rb.linearVelocity;

            bool bounced = false;

            if (pos.x < Constants.EnvironmentConstants.X_MIN)
            {
                pos.x = Constants.EnvironmentConstants.X_MIN;
                vel.x *= -1;
                bounced = true;
            }
            else if (pos.x > Constants.EnvironmentConstants.X_MAX)
            {
                pos.x = Constants.EnvironmentConstants.X_MAX;
                vel.x *= -1;
                bounced = true;
            }

            if (pos.y < Constants.EnvironmentConstants.Y_MIN)
            {
                pos.y = Constants.EnvironmentConstants.Y_MIN;
                vel.y *= -1;
                bounced = true;
            }
            else if (pos.y > Constants.EnvironmentConstants.Y_MAX)
            {
                pos.y = Constants.EnvironmentConstants.Y_MAX;
                vel.y *= -1;
                bounced = true;
            }

            if (bounced)
            {
                BounceOff(vel, pos);
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            ContactPoint2D contact = col.contacts[0];
            float dotProduct = Vector2.Dot(_lastVelocity.normalized, -contact.normal);
            if (dotProduct > 0.1f)
            {
                reflectVelocity = Vector2.Reflect(_lastVelocity, contact.normal);
                BounceOff(reflectVelocity, transform.position);
            }
        }

        private void BounceOff(Vector2 velocity, Vector2 position)
        {
            _rb.linearVelocity = velocity;
            transform.position = position;
            _moveDirection = velocity.normalized;
            _rippleParticleEmitter?.EmitParticles();
            OnBounce?.Invoke(_moveDirection);
        }
        
#region Debug

        private Vector2 reflectVelocity;
        // private void OnDrawGizmos()
        // {
        //     Handles.color = Color.red;
        //     Handles.DrawLine(Vector3.zero, reflectVelocity * 3, 5);
        // }

#endregion
    }
}
