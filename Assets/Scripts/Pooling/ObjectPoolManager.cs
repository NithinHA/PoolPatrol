using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Pooling
{
    /// <summary>
    /// Central singleton that owns one ObjectPool per PoolableItemType.
    /// Any system can call SpawnItem / ReleaseItem without knowing pool internals.
    /// Drop this MonoBehaviour into the scene once (e.g. on a PoolManager GameObject).
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        [Serializable]
        public class PoolConfig
        {
            public PoolableItemType Type;
            public GameObject Prefab;
            public int DefaultCapacity = 10;
            public int MaxSize = 50;
        }

        [SerializeField] private List<PoolConfig> m_PoolConfigs = new();

        private readonly Dictionary<PoolableItemType, ObjectPool<IPoolableObject>> m_Pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (PoolConfig config in m_PoolConfigs)
            {
                PoolConfig capturedConfig = config; // capture for lambda
                GameObject poolContainer = new GameObject($"Pool_{config.Type}");
                poolContainer.transform.SetParent(transform);

                var pool = new ObjectPool<IPoolableObject>(
                    createFunc: () => CreatePooledObject(capturedConfig, poolContainer.transform),
                    actionOnGet: OnGetObject,
                    actionOnRelease: OnReleaseObject,
                    actionOnDestroy: OnDestroyObject,
                    collectionCheck: true,
                    defaultCapacity: capturedConfig.DefaultCapacity,
                    maxSize: capturedConfig.MaxSize
                );

                m_Pools[config.Type] = pool;
            }
        }

        private IPoolableObject CreatePooledObject(PoolConfig config, Transform parent)
        {
            GameObject go = Instantiate(config.Prefab, parent);
            go.SetActive(false);

            if (!go.TryGetComponent(out IPoolableObject poolable))
            {
                Debug.LogError($"[PoolManager] Prefab '{config.Prefab.name}' is missing an IPoolableObject component.");
                return null;
            }

            poolable.Initialize(config.Type);
            return poolable;
        }

        private void OnGetObject(IPoolableObject poolable)
        {
            ((MonoBehaviour)poolable).gameObject.SetActive(true);
            poolable.Reset();
        }

        private void OnReleaseObject(IPoolableObject poolable)
        {
            ((MonoBehaviour)poolable).gameObject.SetActive(false);
        }

        private void OnDestroyObject(IPoolableObject poolable)
        {
            if (poolable != null)
                Destroy(((MonoBehaviour)poolable).gameObject);
        }

        /// <summary>
        /// Retrieves a pooled object of the given type, positioned and rotated as specified.
        /// </summary>
        public IPoolableObject SpawnItem(PoolableItemType type, Vector3 position, Quaternion rotation)
        {
            if (!m_Pools.TryGetValue(type, out var pool))
            {
                Debug.LogError($"[PoolManager] No pool configured for type: {type}");
                return null;
            }

            IPoolableObject item = pool.Get();
            ((MonoBehaviour)item).transform.SetPositionAndRotation(position, rotation);
            return item;
        }

        /// <summary>
        /// Returns a pooled object. Called by the object itself via IPoolableObject.ReturnToPool().
        /// </summary>
        public void ReleaseItem(PoolableItemType type, IPoolableObject item)
        {
            if (!m_Pools.TryGetValue(type, out var pool))
            {
                Debug.LogError($"[PoolManager] No pool configured for type: {type}");
                return;
            }

            pool.Release(item);
        }
    }
}
