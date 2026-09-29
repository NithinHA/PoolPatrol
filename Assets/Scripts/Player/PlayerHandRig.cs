using UnityEngine;

namespace Player
{
    /// <summary>
    /// Visually bridges the player's head to the weapon's grip point with a single rigid "arm"
    /// sprite. No IK is needed: <see cref="PlayerWeaponController"/> rotates WeaponHolder as a
    /// whole, and the grip point (WeaponHolder/Parent) sits at a fixed local offset from it, so
    /// the shoulder-to-grip vector never changes in WeaponHolder's local space — it just rotates
    /// rigidly along with the weapon. This component only needs to align itself once (in Awake,
    /// and live in the editor via OnValidate) rather than every frame.
    /// </summary>
    [ExecuteAlways]
    public class PlayerHandRig : MonoBehaviour
    {
        [Tooltip("The shoulder joint — typically WeaponHolder itself, near the head.")]
        [SerializeField] private Transform m_ShoulderPivot;

        [Tooltip("The grip point the hand reaches for — typically WeaponHolder/Parent.")]
        [SerializeField] private Transform m_GripPoint;

        [Tooltip("Sprite drawn with its pivot at the shoulder end, extending along local +X.")]
        [SerializeField] private SpriteRenderer m_HandRenderer;

#region Unity callbacks

        private void Awake()
        {
            Align();
        }

        private void OnValidate()
        {
            Align();
        }

#endregion

        /// <summary>
        /// Positions this transform at the shoulder, rotates it to face the grip point, and
        /// scales it along local X so the sprite's authored length exactly bridges the gap.
        /// </summary>
        private void Align()
        {
            if (m_ShoulderPivot == null || m_GripPoint == null || m_HandRenderer == null || m_HandRenderer.sprite == null)
                return;

            transform.position = m_ShoulderPivot.position;

            Vector2 toGrip = m_GripPoint.position - m_ShoulderPivot.position;
            float distance = toGrip.magnitude;
            if (distance <= Mathf.Epsilon)
                return;

            float angle = Mathf.Atan2(toGrip.y, toGrip.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            float spriteLength = m_HandRenderer.sprite.bounds.size.x;
            transform.localScale = spriteLength > Mathf.Epsilon
                ? new Vector3(distance / spriteLength, distance / spriteLength, 1f)
                : Vector3.one;
        }
    }
}
