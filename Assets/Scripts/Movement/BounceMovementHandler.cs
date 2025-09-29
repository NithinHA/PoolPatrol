using UnityEditor;
using UnityEngine;

namespace Movement
{
    /// <summary>
    /// This is a separate Bounce off the wall movement handler that is to be applied to entities that do not have movement similar to player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BounceMovementHandler : MonoBehaviour
    {
        public bool DidBounce { get; private set; }
        public Vector2 NewVelocity { get; private set; }
        public Vector2 NewPosition { get; private set; }

        private Vector2 _lastVelocity;
        private ParticleEmitter _rippleParticleEmitter = null;


#region Default callbacks

        public void Tick()
        {
            CheckScreenBounce();
        }

        public void FixedTick(Vector2 velocity)
        {
            DidBounce = false;
            _lastVelocity = velocity;
        }

#endregion

        public void AssignParticleEmitter(ParticleEmitter emitter)
        {
            _rippleParticleEmitter = emitter;
        }

        private void CheckScreenBounce()
        {
            Vector2 pos = transform.position;
            Vector2 vel = _lastVelocity;
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
            if (col.contactCount == 0)
                return;

            ContactPoint2D contact = col.contacts[0];
            float dotProduct = Vector2.Dot(_lastVelocity.normalized, -contact.normal);
            // reflected bounce
            if (dotProduct > 0.1f)
            {
                Vector2 reflectVelocity = Vector2.Reflect(_lastVelocity, contact.normal);
                BounceOff(reflectVelocity, transform.position);
            }
            // rear hit; propulsion
            else
            {
                Rigidbody2D other = col.rigidbody;
                Vector2 otherVelocity = other != null ? other.linearVelocity : Vector2.zero;
                Vector2 relativeVelocity = otherVelocity - _lastVelocity;
                Vector2 newVelocity = _lastVelocity + relativeVelocity * 3;
                BoostForward(newVelocity, transform.position);
            }
        }

        private void BounceOff(Vector2 velocity, Vector2 position)
        {
            DidBounce = true;
            NewVelocity = velocity;
            NewPosition = position;
            _rippleParticleEmitter?.EmitParticles();
        }

        private void BoostForward(Vector2 velocity, Vector2 position)
        {
            DidBounce = true;
            NewVelocity = velocity;
            NewPosition = position;
            _rippleParticleEmitter?.EmitParticles(3);
        }
        
#if UNITY_EDITOR
#region Debug
        
        private void OnDrawGizmos()
        {
            Handles.color = Color.red;
            Handles.DrawLine(NewPosition, NewPosition + NewVelocity.normalized, 3);
        }

#endregion
#endif
    }
}