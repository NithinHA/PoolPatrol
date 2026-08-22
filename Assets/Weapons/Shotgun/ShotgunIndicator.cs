using UnityEngine;

namespace Weapon
{
    [RequireComponent(typeof(LineRenderer))]
    public class ShotgunIndicator : ProjectileIndicatorBase
    {
        [SerializeField] private LineRenderer m_LineRenderer;

        [Header("Collides enemy")]
        [SerializeField] private LayerMask m_EnemyLayerMask;

        [Header("Width")]
        [SerializeField] private float m_NormalWidth = 0.4f;
        [SerializeField] private float m_EnemyHitWidth = 0.8f;

        /// <summary>Total angle range (e.g. 24 degrees = -12 to +12 from center).</summary>
        public float AngleRange { get; set; } = 24f;

        private void Awake()
        {
            if (m_LineRenderer == null)
                m_LineRenderer = GetComponent<LineRenderer>();

            m_LineRenderer.useWorldSpace = true;
            SetWidth(m_NormalWidth);
        }

        public override void UpdateAim(Vector2 origin, Vector2 direction, float range)
        {
            float halfAngle = AngleRange * 0.5f;
            Vector2 leftDir  = Quaternion.Euler(0, 0,  halfAngle) * direction;
            Vector2 rightDir = Quaternion.Euler(0, 0, -halfAngle) * direction;

            m_LineRenderer.positionCount = 3;
            m_LineRenderer.SetPosition(0, origin + leftDir  * range);
            m_LineRenderer.SetPosition(1, origin);
            m_LineRenderer.SetPosition(2, origin + rightDir * range);

            if (!CanShoot)
                return;

            bool hitsEnemy = CheckEnemyInCone(origin, direction, range, halfAngle);
            SetWidth(hitsEnemy ? m_EnemyHitWidth : m_NormalWidth);
            ApplyColor(hitsEnemy ? m_HighlightColor : m_EnableColor);
        }

        protected override void ApplyColor(Color color)
        {
            m_LineRenderer.startColor = color;
            m_LineRenderer.endColor   = color;
        }

        protected override void OnCanShootChanged(bool canShoot)
        {
            if (!canShoot)
                ResetVisuals();
        }

        public override void Hide()
        {
            ResetVisuals();
            base.Hide();
        }

        private bool CheckEnemyInCone(Vector2 origin, Vector2 direction, float range, float halfAngle)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, m_EnemyLayerMask);
            foreach (Collider2D col in hits)
            {
                Vector2 toEnemy = (Vector2)col.transform.position - origin;
                if (Vector2.Angle(direction, toEnemy) <= halfAngle)
                    return true;
            }
            return false;
        }

        private void SetWidth(float width)
        {
            m_LineRenderer.startWidth = width;
            m_LineRenderer.endWidth   = width;
        }

        private void ResetVisuals()
        {
            SetWidth(m_NormalWidth);
        }
    }
}
