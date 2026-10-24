using UnityEngine;

/// <summary>
/// Fakes height for decor seen from straight above: the visual leans away from the camera centre in
/// proportion to how tall it is, so plants, umbrellas and foreground leaves slide over the deck as the
/// camera follows the player. Put it on the root and point it at the sprite to move.
/// </summary>
public class HeightParallax : MonoBehaviour
{
    [SerializeField] private Transform m_Visual;
    [Tooltip("0 = flat on the ground. ~0.5 for a potted plant, ~2 for foreground leaves near the lens.")]
    [SerializeField] private float m_Height = 0.5f;
    [Tooltip("World units of lean per unit of height per unit of distance from the camera centre.")]
    [SerializeField] private float m_Strength = 0.04f;

    private Camera _camera;

    private void LateUpdate()
    {
        if (m_Visual == null) return;
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        Vector2 fromCamera = transform.position - _camera.transform.position;
        m_Visual.position = transform.position + (Vector3)(fromCamera * (m_Height * m_Strength));
    }
}
