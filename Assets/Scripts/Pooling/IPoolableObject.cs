namespace Pooling
{
    /// <summary>
    /// Contract for every object managed by the PoolManager.
    /// Implementations are typically MonoBehaviours attached to pooled prefabs.
    /// </summary>
    public interface IPoolableObject
    {
        /// <summary>
        /// Called once when the object is first created (PoolManager.createFunc).
        /// Store the type so the object can self-return to the correct pool.
        /// </summary>
        void Initialize(PoolableItemType type);

        /// <summary>
        /// Called every time the object is retrieved from the pool (PoolManager.actionOnGet).
        /// Restore the object to a clean, ready-to-use state.
        /// </summary>
        void Reset();

        /// <summary>
        /// Called by the object itself when it has finished its work.
        /// Internally delegates to PoolManager.Instance.ReleaseItem().
        /// </summary>
        void ReturnToPool();
    }
}
