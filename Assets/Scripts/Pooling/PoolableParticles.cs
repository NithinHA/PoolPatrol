using UnityEngine;

namespace Pooling
{
    /// <summary>
    /// Attach to any particle system prefab that should be pooled.
    /// Automatically returns itself to the PoolManager when the particle system stops.
    /// Setup on the prefab: Set ParticleSystem StopAction to "None" (we handle return via code)
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PoolableParticles : MonoBehaviour, IPoolableObject
    {
        public ParticleSystem Particles;

        private PoolableItemType _itemType;

        /// <summary>
        /// Called once on first creation. Caches the type for later self-return.
        /// </summary>
        public void Initialize(PoolableItemType type)
        {
            if (Particles == null)
                Particles = GetComponent<ParticleSystem>();
            _itemType = type;

            // Point the stop callback at our named method — no lambda, no closure leak.
            ParticleSystem.MainModule main = Particles.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        /// <summary>
        /// Called every time the object is retrieved. Replays the particle system cleanly.
        /// </summary>
        public void Reset()
        {
            Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Particles.Play();
        }

        /// <summary>
        /// Called by the object itself to return to the pool.
        /// </summary>
        public void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReleaseItem(_itemType, this);
        }

        /// <summary>
        /// Unity calls this (via SendMessage) when the ParticleSystem finishes, because stopAction is set to Callback.
        /// Using SendMessage avoids the need for a delegate.
        /// </summary>
        private void OnParticleSystemStopped()
        {
            if (gameObject.activeSelf)
                ReturnToPool();
        }
    }
}
