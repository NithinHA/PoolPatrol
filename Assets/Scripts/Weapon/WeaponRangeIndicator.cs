using UnityEngine;

namespace Weapon
{
    [RequireComponent(typeof(LineRenderer))]
    public class WeaponRangeIndicator : MonoBehaviour
    {
        [SerializeField] private LineRenderer m_LineRenderer;
        [SerializeField] private int m_Segments = 50;
        
        private void Awake()
        {
            if (m_LineRenderer == null)
                m_LineRenderer = GetComponent<LineRenderer>();
            
            m_LineRenderer.useWorldSpace = false;
            m_LineRenderer.loop = true;
        }

        public void SetRange(float range)
        {
            m_LineRenderer.positionCount = m_Segments;
            
            for (int i = 0; i < m_Segments; i++)
            {
                float angle = (float)i / m_Segments * 2f * Mathf.PI;
                float x = Mathf.Cos(angle) * range;
                float y = Mathf.Sin(angle) * range;
                m_LineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
