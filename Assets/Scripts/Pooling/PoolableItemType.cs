namespace Pooling
{
    /// <summary>
    /// Identifies each type of object that the PoolManager can pool.
    /// Add a new entry here for every new poolable prefab.
    /// </summary>
    public enum PoolableItemType
    {
        Crosshair,
        EnemySpawnParticles,
        BulletHitParticles,
        BulletComboExplosion,
        BulletMissedParticles,
    }
}
