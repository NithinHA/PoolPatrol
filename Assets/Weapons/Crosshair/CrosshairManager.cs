using UnityEngine;
using UnityEngine.Pool;

namespace Weapon
{
    public class CrosshairManager : MonoBehaviour
    {
        public static CrosshairManager Instance { get; private set; }

        [SerializeField] private Crosshair m_CrosshairPrefab;
        [SerializeField] private int m_DefaultCapacity = 10;
        [SerializeField] private int m_MaxSize = 50;

        private ObjectPool<Crosshair> _crosshairPool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;

            _crosshairPool = new ObjectPool<Crosshair>(
                createFunc: CreateCrosshair,
                actionOnGet: OnGetCrosshair,
                actionOnRelease: OnReleaseCrosshair,
                actionOnDestroy: OnDestroyCrosshair,
                collectionCheck: true,
                defaultCapacity: m_DefaultCapacity,
                maxSize: m_MaxSize
            );
        }

        private Crosshair CreateCrosshair()
        {
            Crosshair crosshair = Instantiate(m_CrosshairPrefab, transform);
            crosshair.gameObject.SetActive(false);
            return crosshair;
        }

        /// <summary>
        /// Gets you a clean reset crosshair object that's ready to use again.
        /// </summary>
        private void OnGetCrosshair(Crosshair crosshair)
        {
            crosshair.gameObject.SetActive(true);
            crosshair.ResetCrosshair();
        }

        private void OnReleaseCrosshair(Crosshair crosshair)
        {
            crosshair.gameObject.SetActive(false);
        }

        private void OnDestroyCrosshair(Crosshair crosshair)
        {
            if (crosshair != null)
                Destroy(crosshair.gameObject);
        }

        public Crosshair SpawnCrosshair(Vector2 position)
        {
            Crosshair crosshair = _crosshairPool.Get();
            crosshair.transform.position = position;
            return crosshair;
        }

        public void ReleaseCrosshair(Crosshair crosshair)
        {
            _crosshairPool.Release(crosshair);
        }
    }
}
