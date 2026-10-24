using UnityEngine;

/// <summary>
/// Everything that sells "this thing is floating in the water", for one object:
///  - a soft shadow on the pool floor, offset along the sun (<see cref="PoolEnvironment"/>),
///  - procedural rings where it meets the water,
///  - an optional gentle bob of a visual-only transform.
/// The shadow and ring are spawned as children at runtime and posed in world space each LateUpdate,
/// so they ignore the object's own spin and scale. Pool-friendly: nothing is created twice.
/// </summary>
public class WaterContactFX : MonoBehaviour
{
    [Header("Underwater Shadow")]
    [SerializeField] private bool m_CastShadow = true;
    [Tooltip("Sprite whose silhouette becomes the shadow. Leave empty to use the blob sprite instead.")]
    [SerializeField] private SpriteRenderer m_ShadowSource;
    [SerializeField] private Sprite m_BlobSprite;
    [Tooltip("Blob size in world units. Ignored when a shadow source is set.")]
    [SerializeField] private Vector2 m_BlobSize = Vector2.one;
    [SerializeField] private Material m_ShadowMaterial;
    [SerializeField, Range(0f, 1f)] private float m_ShadowStrength = 1f;
    [Tooltip("Multiplies the scene-wide shadow offset. Raise it for things that sit higher out of the water.")]
    [SerializeField] private float m_ShadowHeight = 1f;
    [SerializeField] private float m_ShadowScale = 1.05f;

    [Header("Waterline Ring")]
    [SerializeField] private bool m_ShowRing = true;
    [Tooltip("Full-rect quad sprite. The ring shader ignores its pixels.")]
    [SerializeField] private Sprite m_RingSprite;
    [SerializeField] private Material m_RingMaterial;
    [Tooltip("Width of the object at the waterline, in world units.")]
    [SerializeField] private float m_RingDiameter = 1f;
    [SerializeField, Range(0f, 1f)] private float m_RingIntensity = 1f;

    [Header("Bob")]
    [Tooltip("Visual-only transform to bob. Never the physics root.")]
    [SerializeField] private Transform m_BobTarget;
    [SerializeField] private float m_BobAmplitude = 0.035f;
    [SerializeField] private float m_BobFrequency = 0.6f;

    [Header("Sorting")]
    [SerializeField] private string m_SortingLayer = "Water";
    [Tooltip("Must sit between the floor (opaque) and the water surface.")]
    [SerializeField] private int m_ShadowOrder = 0;
    [Tooltip("Must sit above the water surface and below the deck.")]
    [SerializeField] private int m_RingOrder = 20;

    // Ring quad diameter relative to the waterline diameter; matches the shader's contact radius of 0.5.
    private const float RingQuadScale = 2f;

    private SpriteRenderer _shadow;
    private SpriteRenderer _ring;
    private Vector3 _bobBase;
    private float _bobPhase;

#region Unity callbacks

    private void Awake()
    {
        _bobPhase = Random.value * Mathf.PI * 2f;
        if (m_BobTarget != null) _bobBase = m_BobTarget.localPosition;

        if (m_CastShadow)
        {
            if (m_ShadowMaterial == null)
                Debug.LogWarning($"[WaterContactFX] {name} has no shadow material.");
            else
                _shadow = CreateChild("UnderwaterShadow", m_ShadowMaterial, m_ShadowOrder,
                                      m_ShadowSource != null ? m_ShadowSource.sprite : m_BlobSprite,
                                      new Color(1f, 1f, 1f, m_ShadowStrength));
        }

        if (m_ShowRing)
        {
            if (m_RingMaterial == null || m_RingSprite == null)
                Debug.LogWarning($"[WaterContactFX] {name} is missing its ring material or sprite.");
            else
                // Red carries a per-object phase so neighbouring rings don't pulse in step.
                _ring = CreateChild("WaterlineRing", m_RingMaterial, m_RingOrder, m_RingSprite,
                                    new Color(Random.value, 1f, 1f, m_RingIntensity));
        }
    }

    private void OnDisable()
    {
        if (m_BobTarget != null) m_BobTarget.localPosition = _bobBase;
    }

    private void LateUpdate()
    {
        if (_shadow != null) UpdateShadow();
        if (_ring != null) UpdateRing();

        if (m_BobTarget != null)
        {
            float s = Mathf.Sin(Time.time * m_BobFrequency * Mathf.PI * 2f + _bobPhase);
            m_BobTarget.localPosition = _bobBase + new Vector3(0f, s * m_BobAmplitude, 0f);
        }
    }

#endregion

    private SpriteRenderer CreateChild(string childName, Material material, int order, Sprite sprite, Color color)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = material;
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = m_SortingLayer;
        sr.sortingOrder = order;
        return sr;
    }

    private void UpdateShadow()
    {
        Transform t = _shadow.transform;
        Vector3 offset = PoolEnvironment.ObjectShadowOffset * m_ShadowHeight;

        if (m_ShadowSource != null)
        {
            _shadow.enabled = m_ShadowSource.enabled && m_ShadowSource.gameObject.activeInHierarchy;
            _shadow.sprite = m_ShadowSource.sprite;
            _shadow.flipX = m_ShadowSource.flipX;
            _shadow.flipY = m_ShadowSource.flipY;
            Transform src = m_ShadowSource.transform;
            t.SetPositionAndRotation(src.position + offset, src.rotation);
            SetWorldScale(t, src.lossyScale * m_ShadowScale);
        }
        else if (m_BlobSprite != null)
        {
            t.SetPositionAndRotation(transform.position + offset, Quaternion.identity);
            Vector2 spriteSize = m_BlobSprite.bounds.size;
            SetWorldScale(t, new Vector3(m_BlobSize.x / spriteSize.x, m_BlobSize.y / spriteSize.y, 1f) * m_ShadowScale);
        }
    }

    private void UpdateRing()
    {
        Transform t = _ring.transform;
        t.SetPositionAndRotation(transform.position, Quaternion.identity);
        Vector2 spriteSize = m_RingSprite.bounds.size;
        float d = m_RingDiameter * RingQuadScale;
        SetWorldScale(t, new Vector3(d / spriteSize.x, d / spriteSize.y, 1f));
    }

    private static void SetWorldScale(Transform t, Vector3 worldScale)
    {
        Vector3 parent = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(
            parent.x != 0f ? worldScale.x / parent.x : 0f,
            parent.y != 0f ? worldScale.y / parent.y : 0f,
            1f);
    }
}
