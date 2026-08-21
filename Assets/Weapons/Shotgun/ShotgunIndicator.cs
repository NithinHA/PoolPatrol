using UnityEngine;

namespace Weapon
{
    [RequireComponent(typeof(LineRenderer))]
    public class ShotgunIndicator : ProjectileIndicatorBase
    {
        [SerializeField] private LineRenderer m_LineRenderer;
        
        /// <summary>
        /// Total angle range (e.g. 30 degrees = -15 to +15 from center vector).
        /// </summary>
        public float AngleRange { get; set; } = 24f; // Default angle

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
            float halfAngle = AngleRange * 0.5f;
            
            // Calculate left and right direction vectors
            Vector2 leftDir = Quaternion.Euler(0, 0, halfAngle) * direction;
            Vector2 rightDir = Quaternion.Euler(0, 0, -halfAngle) * direction;

            // Draw: Left End -> Origin -> Right End
            m_LineRenderer.positionCount = 3;
            m_LineRenderer.SetPosition(0, origin + leftDir * range);
            m_LineRenderer.SetPosition(1, origin);
            m_LineRenderer.SetPosition(2, origin + rightDir * range);
        }

        public override void UpdateColor(bool canShoot)
        {
            Color targetColor = canShoot ? m_EnableColor : m_DisableColor;
            m_LineRenderer.startColor = targetColor;
            m_LineRenderer.endColor = targetColor;
        }
    }
}
