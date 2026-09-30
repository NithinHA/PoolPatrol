using UnityEngine;

namespace UI.HUD
{
    /// <summary>
    /// Arranges this transform's direct RectTransform children evenly around a
    /// ring (or fan) of a given radius, spacing each subsequent child by a
    /// fixed angle from the previous one. Attach to BulletContainer so
    /// BulletIcon instances fan out around the weapon icon instead of
    /// stacking linearly. Re-arranges automatically whenever a child is
    /// added or removed.
    /// </summary>
    [ExecuteAlways]
    public class BulletRingLayout : MonoBehaviour
    {
        [SerializeField] private float m_Radius = 90f;
        [SerializeField] private float m_StartAngle = -75f;   // degrees, 0 = +X axis, 90 = up
        [SerializeField] private float m_AngleBetween = 30f;  // angle step from one bullet to the next
        [SerializeField] private bool m_Clockwise = false;

        private void OnTransformChildrenChanged() => Arrange();

        private void OnValidate() => Arrange();

        public void Arrange()
        {
            int count = transform.childCount;
            if (count == 0)
                return;

            float direction = m_Clockwise ? -1f : 1f;

            for (int i = 0; i < count; i++)
            {
                RectTransform child = transform.GetChild(i) as RectTransform;
                if (child == null)
                    continue;

                float angleRad = (m_StartAngle + direction * m_AngleBetween * i) * Mathf.Deg2Rad;
                child.anchoredPosition = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * m_Radius;
            }
        }
    }
}
