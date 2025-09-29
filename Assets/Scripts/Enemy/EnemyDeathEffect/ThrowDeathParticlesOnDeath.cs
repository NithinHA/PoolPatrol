using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Death
{
    public class ThrowDeathParticlesOnDeath: MonoBehaviour, IOnDeathEffect
    {
        [SerializeField] private ParticleSystem m_DeathParticlesPrefab;
        [SerializeField] private Color m_DeathParticlesColor = Color.white;

        public void Execute(Dictionary<string, object> parameters)
        {
            Collision2D collision = parameters.ContainsKey(Constants.GameConstants.BULLET_COLLISION_Collider)
                ? parameters[Constants.GameConstants.BULLET_COLLISION_Collider] as Collision2D : null;
            if (collision == null)
            {
                Failure();
                return;
            }
            Vector3? bulletRbDirection = parameters.ContainsKey(Constants.GameConstants.BULLET_COLLISION_Direction)
                ? parameters[Constants.GameConstants.BULLET_COLLISION_Direction] as Vector3? : null;
            if (bulletRbDirection == null)
            {
                Failure();
                return;
            }

            ParticleSystem deathParticles = Instantiate(m_DeathParticlesPrefab, transform.position, Quaternion.identity);
            SetParticleColor(deathParticles);
            SetParticleThrowDirection(deathParticles, bulletRbDirection.Value);
            deathParticles.Play();
        }

        private void Failure()
        {
            Debug.Log("Something went wrong ni ThrowDeathParticlesOnDeath!");
        }

        private void SetParticleColor(ParticleSystem deathParticles)
        {
            ParticleSystem.MainModule mainComponent = deathParticles.main;
            mainComponent.startColor = m_DeathParticlesColor;
        }
        
        private void SetParticleThrowDirection(ParticleSystem deathParticles, Vector3 bulletRbDirection)
        {
            Quaternion lookRotation = Quaternion.LookRotation(bulletRbDirection);
            Vector3 eulerAngles = lookRotation.eulerAngles;

            ParticleSystem.ShapeModule shape = deathParticles.shape;
            Vector3 currentRotation = shape.rotation;
            shape.rotation = new Vector3(eulerAngles.x, eulerAngles.y, currentRotation.z);
        }
    }
}