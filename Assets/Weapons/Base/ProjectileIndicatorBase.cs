using UnityEngine;

namespace Weapon
{
    public abstract class ProjectileIndicatorBase : MonoBehaviour
    {
        [SerializeField] protected Color m_EnableColor = Color.white;
        [SerializeField] protected Color m_DisableColor = Color.red;

        public virtual void Show(bool canShoot)
        {
            gameObject.SetActive(true);
            UpdateColor(canShoot);
        }
        
        public virtual void Hide() => gameObject.SetActive(false);

        public abstract void UpdateAim(Vector2 origin, Vector2 direction, float range);
        
        public abstract void UpdateColor(bool canShoot);
    }
}
