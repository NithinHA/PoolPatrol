using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Death
{
    public class ThrowDeathParticlesOnDeath: MonoBehaviour, IOnDeathEffect
    {
        [SerializeField] private ParticleSystem m_DeathParticlesPrefab;
        [SerializeField] private Color m_DeathParticlesColor = Color.white;

        public void Execute(EnemyDeathParameters parameters)
        {
            if (parameters.CollidingObject == null)
            {
                Failure();
                return;
            }

            ParticleSystem deathParticles = Instantiate(m_DeathParticlesPrefab, transform.position, Quaternion.identity);
            SetParticleColor(deathParticles);
            SetParticleThrowDirection(deathParticles, parameters.CollisionDirection);
            deathParticles.Play();
        }

        private void Failure()
        {
            Debug.Log("Something went wrong in ThrowDeathParticlesOnDeath!");
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