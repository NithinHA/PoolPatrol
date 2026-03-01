using UnityEngine;

namespace Pooling
{
    /// <summary>
    /// Attach to any particle system prefab that should be pooled.
    /// Automatically returns itself to the PoolManager when the particle system stops.
    ///
    /// Setup on the prefab:
    ///   - ParticleSystem > Stop Action: set to "None" (we handle return via code).
    ///   - ParticleSystem > Send Collision Messages: can remain as-is.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PoolableParticles : MonoBehaviour, IPoolableObject
    {
        [SerializeField] private ParticleSystem m_ParticleSystem;

        private PoolableItemType _itemType;

        // ── IPoolableObject ──────────────────────────────────────────────────

        /// <summary>
        /// Called once on first creation. Caches the type for later self-return.
        /// </summary>
        public void Initialize(PoolableItemType type)
        {
            m_ParticleSystem = GetComponent<ParticleSystem>();
            _itemType = type;

            // Point the stop callback at our named method — no lambda, no closure leak.
            ParticleSystem.MainModule main = m_ParticleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        /// <summary>
        /// Called every time the object is retrieved. Replays the particle system cleanly.
        /// </summary>
        public void Reset()
        {
            m_ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_ParticleSystem.Play();
        }

        /// <summary>
        /// Called by the object itself to return to the pool.
        /// </summary>
        public void ReturnToPool()
        {
            ObjectPoolManager.Instance.ReleaseItem(_itemType, this);
        }

        // ── ParticleSystem callback ──────────────────────────────────────────

        /// <summary>
        /// Unity calls this (via SendMessage) when the ParticleSystem finishes,
        /// because stopAction is set to Callback. Using SendMessage avoids the need
        /// for a delegate — no closure, no retained reference.
        /// </summary>
        private void OnParticleSystemStopped()
        {
            // Guard: only return if we're still active (guard against late callbacks
            // after the object was already released by some other code path).
            if (gameObject.activeSelf)
                ReturnToPool();
        }
    }
}
