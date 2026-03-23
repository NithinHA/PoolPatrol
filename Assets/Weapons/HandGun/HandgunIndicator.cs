using UnityEngine;

namespace Weapon
{
    [RequireComponent(typeof(LineRenderer))]
    public class HandgunIndicator : ProjectileIndicatorBase
    {
        [SerializeField] private LineRenderer m_LineRenderer;

        private void Awake()
        {
            if (m_LineRenderer == null)
            {
                m_LineRenderer = GetComponent<LineRenderer>();
            }
            m_LineRenderer.useWorldSpace = true;
        }

        public override void UpdateAim(Vector2 origin, Vector2 direction, float range)
        {
            m_LineRenderer.positionCount = 2;
            m_LineRenderer.SetPosition(0, origin);
            m_LineRenderer.SetPosition(1, origin + direction * range);
        }

        public override void UpdateColor(bool canShoot)
        {
            Color targetColor = canShoot ? m_EnableColor : m_DisableColor;
            m_LineRenderer.startColor = targetColor;
            m_LineRenderer.endColor = targetColor;
        }
    }
}
