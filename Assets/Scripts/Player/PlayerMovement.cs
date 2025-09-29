using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float m_ImpulseStrength = 12f;
        [SerializeField] private float m_TargetSpeed = 3f;
        [SerializeField] private float m_SmoothingTime = 0.2f;

        [Header("Screen Bounds")]
        [SerializeField] private float _xMin = -9f;
        [SerializeField] private float _xMax = 9f, _yMin = -5f, _yMax = 5f;
        
        [Header("Referrences")]
        [SerializeField] private PlayerController m_PlayerController;
        
        private Rigidbody2D _rb;
        private Vector2 _moveDirection;
        private float _smoothingTimer;
        private bool _isDecaying;
        private Vector2 _lastVelocity;

#region Unity callbacks

        void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.linearDamping = 0;

            m_PlayerController.OnFireInput += OnFireInput;
        }

        void Update()
        {
            if (!_isDecaying) // Keep velocity steady after decay
            {
                _rb.linearVelocity = _moveDirection * m_TargetSpeed;
            }

            CheckScreenBounce();
        }

        void FixedUpdate()
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

#endregion

        private void OnFireInput(Vector2 fireDirection)
        {
            ApplyImpulse(-fireDirection);
        }
        
        void ApplyImpulse(Vector2 direction)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(direction * m_ImpulseStrength, ForceMode2D.Impulse);
            _moveDirection = direction.normalized;
            _smoothingTimer = 0f;
            _isDecaying = true;
        }

        void CheckScreenBounce()
        {
            Vector2 pos = transform.position;
            Vector2 vel = _rb.linearVelocity;

            bool bounced = false;

            if (pos.x < _xMin)
            {
                pos.x = _xMin;
                vel.x *= -1;
                bounced = true;
            }
            else if (pos.x > _xMax)
            {
                pos.x = _xMax;
                vel.x *= -1;
                bounced = true;
            }

            if (pos.y < _yMin)
            {
                pos.y = _yMin;
                vel.y *= -1;
                bounced = true;
            }
            else if (pos.y > _yMax)
            {
                pos.y = _yMax;
                vel.y *= -1;
                bounced = true;
            }

            if (bounced)
            {
                BounceOff(vel, pos);
                m_PlayerController.WaterRippleParticleEmitter.EmitParticles();
            }
        }


        private void OnCollisionEnter2D(Collision2D col)
        {
            ContactPoint2D contact = col.contacts[0];
            // Vector2 movementDir = (contact.point - (Vector2) transform.position).normalized;
            reflectVelocity = Vector2.Reflect(_lastVelocity, contact.normal);
            BounceOff(reflectVelocity, transform.position);
            m_PlayerController.WaterRippleParticleEmitter.EmitParticles();
        }

        private void BounceOff(Vector2 velocity, Vector2 position)
        {
            _rb.linearVelocity = velocity;
            transform.position = position;
            _moveDirection = velocity.normalized;
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