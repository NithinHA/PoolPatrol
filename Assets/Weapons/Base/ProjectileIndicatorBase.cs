using UnityEngine;

namespace Weapon
{
    public abstract class ProjectileIndicatorBase : MonoBehaviour
    {
        [SerializeField] protected Color m_EnableColor = Color.white;
        [SerializeField] protected Color m_DisableColor = Color.red;
        [SerializeField] protected Color m_HighlightColor = Color.yellow;

        protected bool CanShoot { get; private set; } = true;

        public virtual void Show(bool canShoot)
        {
            gameObject.SetActive(true);
            UpdateColor(canShoot);
        }

        public virtual void Hide() => gameObject.SetActive(false);

        public abstract void UpdateAim(Vector2 origin, Vector2 direction, float range);

        /// <summary>
        /// Called by the weapon controller when the can-shoot state changes (reload, cooldown).
        /// Applies the appropriate color and notifies subclasses via OnCanShootChanged.
        /// </summary>
        public void UpdateColor(bool canShoot)
        {
            bool changed = canShoot != CanShoot;
            CanShoot = canShoot;
            ApplyColor(canShoot ? m_EnableColor : m_DisableColor);
            if (changed)
                OnCanShootChanged(canShoot);
        }

        /// <summary>Applies the given color to the indicator's renderer(s).</summary>
        protected abstract void ApplyColor(Color color);

        /// <summary>
        /// Called once when CanShoot transitions. Override to reset or apply
        /// indicator-specific state (width, speed, scale, etc.).
        /// </summary>
        protected virtual void OnCanShootChanged(bool canShoot) { }
    }
}
