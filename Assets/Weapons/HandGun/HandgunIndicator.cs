using UnityEngine;

namespace Weapon
{
    [RequireComponent(typeof(LineRenderer))]
    public class HandgunIndicator : ProjectileIndicatorBase
    {
        [SerializeField] private LineRenderer m_LineRenderer;

        [Header("Collides enemy")]
        [SerializeField] private LayerMask m_EnemyLayerMask;
        
        [SerializeField] private float m_NormalWidth = 0.4f;
        [SerializeField] private float m_EnemyHitWidth = 0.8f;
        
        [SerializeField] private float m_NormalSpeed = 1.4f;
        [SerializeField] private float m_HitSpeed = -5f;

        private static readonly int SpeedProperty = Shader.PropertyToID("_Speed");
        private void Awake()
        {
            if (m_LineRenderer == null)
                m_LineRenderer = GetComponent<LineRenderer>();

            m_LineRenderer.useWorldSpace = true;
            SetWidth(m_NormalWidth);
        }

        public override void UpdateAim(Vector2 origin, Vector2 direction, float range)
        {
            m_LineRenderer.positionCount = 2;
            m_LineRenderer.SetPosition(0, origin);
            m_LineRenderer.SetPosition(1, origin + direction * range);

            if (!CanShoot)
                return;

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, range, m_EnemyLayerMask);
            bool hitsEnemy = hit.collider != null;
            SetWidth(hitsEnemy ? m_EnemyHitWidth : m_NormalWidth);
            ApplyColor(hitsEnemy ? m_HighlightColor : m_EnableColor);
            m_LineRenderer.material.SetFloat(SpeedProperty, hitsEnemy ? m_HitSpeed : m_NormalSpeed);
        }

        public override void Hide()
        {
            ResetVisuals();
            base.Hide();
        }

        protected override void ApplyColor(Color color)
        {
            m_LineRenderer.startColor = color;
            m_LineRenderer.endColor = color;
        }

        protected override void OnCanShootChanged(bool canShoot)
        {
            if (!canShoot)
                ResetVisuals();
        }

        private void SetWidth(float width)
        {
            m_LineRenderer.startWidth = width;
            m_LineRenderer.endWidth = width;
        }

        private void ResetVisuals()
        {
            SetWidth(m_NormalWidth);
            m_LineRenderer.material.SetFloat(SpeedProperty, m_NormalSpeed);
        }
    }
}
