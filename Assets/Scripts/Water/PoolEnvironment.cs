using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Single source of truth for the pool's look: the shape of the water area and the sun.
/// Publishes them as shader globals every frame so PoolFloor, PoolSurface, PoolDeck and every
/// <see cref="WaterContactFX"/> agree, in edit mode as well as play mode.
/// The water area is fitted between the four wall colliders, so moving a wall moves the visuals too.
///
/// It also applies the pool's share of the player's graphics quality setting
/// (<see cref="GraphicsQualityProfile"/>, chosen in the Settings popup):
///  - Floor bake: in play mode the static part of PoolFloor (tiles, wall face, AO, rim shadow, depth
///    tint) is rendered once into a texture, so each frame the floor only pays for the moving parts.
///    The profile sets its resolution.
///  - Cheap water shaders (global _POOL_LOW) and bloom on/off, both straight from the profile.
/// </summary>
[ExecuteAlways]
public class PoolEnvironment : MonoBehaviour
{
    /// <summary>Where the quality profile comes from. Force* overrides the player's setting, for testing.</summary>
    public enum QualitySource { Settings, ForceLow, ForceMid, ForceHigh }

    public static PoolEnvironment Instance { get; private set; }

    private static readonly Vector2 DefaultShadowOffset = new Vector2(0.1f, -0.26f);

    private static readonly int PoolRectId = Shader.PropertyToID("_PoolRect");
    private static readonly int PoolShapeId = Shader.PropertyToID("_PoolShape");
    private static readonly int PoolSunId = Shader.PropertyToID("_PoolSun");
    private static readonly int PoolBakedFloorId = Shader.PropertyToID("_PoolBakedFloor");
    private static readonly int PoolBakedRectId = Shader.PropertyToID("_PoolBakedRect");

    private const string BakedKeyword = "_POOL_BAKED";
    private const string LowKeyword = "_POOL_LOW";
    private const string BakePassName = "BakeStatic";

    [Header("Pool Shape")]
    [Tooltip("The water area is the gap between these walls. Leave any empty to use the manual rect.")]
    [SerializeField] private BoxCollider2D m_LeftWall;
    [SerializeField] private BoxCollider2D m_RightWall;
    [SerializeField] private BoxCollider2D m_TopWall;
    [SerializeField] private BoxCollider2D m_BottomWall;
    [SerializeField] private Rect m_ManualRect = new Rect(-13.4f, -8.5f, 26.8f, 17f);
    [SerializeField] private float m_CornerRadius = 0.6f;

    [Header("Sun")]
    [Tooltip("Direction light travels across the pool (screen space). Shadows fall this way.")]
    [SerializeField] private Vector2 m_SunDirection = new Vector2(0.36f, -0.93f);
    [Tooltip("How far the rim shadow reaches into the pool, in world units.")]
    [SerializeField] private float m_RimShadowLength = 0.9f;
    [SerializeField] private float m_RimShadowSoftness = 0.3f;
    [Tooltip("How far an object's underwater shadow falls from it, in world units.")]
    [SerializeField] private float m_ObjectShadowDistance = 0.35f;

    [Header("Performance")]
    [Tooltip("Settings = the player's choice in the Settings popup (edit mode previews High). " +
             "Force* ignores it, for testing a tier in the Editor.")]
    [SerializeField] private QualitySource m_QualitySource = QualitySource.Settings;
    [Tooltip("Bake the static floor layer into a texture in play mode.")]
    [SerializeField] private bool m_BakeFloor = true;
    [Tooltip("The PoolFloor material; its BakeStatic pass renders the bake.")]
    [SerializeField] private Material m_FloorMaterial;
    [Tooltip("World units baked past the water edge, so refraction near the walls stays inside the bake.")]
    [SerializeField] private float m_BakeMargin = 0.5f;

    /// <summary>World-space offset from a floating object to its shadow on the pool floor.</summary>
    public static Vector2 ObjectShadowOffset =>
        Instance != null ? Instance.m_SunDirection.normalized * Instance.m_ObjectShadowDistance : DefaultShadowOffset;

    /// <summary>The water area in world space, as currently published to the shaders.</summary>
    public Rect WaterRect { get; private set; }

    public float CornerRadius => m_CornerRadius;

    /// <summary>The quality profile currently applied to the pool.</summary>
    public GraphicsQualityProfile Profile { get; private set; } = GraphicsQualityProfile.For(GraphicsQuality.High);

    private ISettingsService _settings;
    private RenderTexture _bakedFloor;
    private int _bakedHash;
    private Bloom _bloom;
    private bool _bloomWasActive;

#region Unity callbacks

    private void OnEnable()
    {
        Instance = this;
        if (Application.isPlaying)
        {
            _settings = ServiceLocator.GetSettingsService();
            if (_settings != null) _settings.OnSettingsChanged += OnSettingsChanged;
        }
        Publish();
        ApplyQuality();
        RefreshBake();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (_settings != null) _settings.OnSettingsChanged -= OnSettingsChanged;
        _settings = null;
        ReleaseBake();
        Shader.DisableKeyword(LowKeyword);
        if (_bloom != null) _bloom.active = _bloomWasActive;
    }

    private void Update()
    {
        // Cheap enough to do every frame, and keeps edit-mode wall tweaks live.
        Publish();
        RefreshBake();
    }

    private void OnValidate()
    {
        Publish();
        if (isActiveAndEnabled)
        {
            ApplyQuality();
            RefreshBake();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Rect r = ComputeWaterRect();
        Gizmos.DrawWireCube(r.center, r.size);
    }

#endregion

    /// <summary>Re-renders the floor bake now. Use after changing PoolFloor material values from code.</summary>
    [ContextMenu("Rebake Floor")]
    public void Rebake()
    {
        _bakedHash = 0;
        RefreshBake();
    }

    // Fires for every setting (volume too); the bake only re-renders if its resolution changed.
    private void OnSettingsChanged()
    {
        ApplyQuality();
        RefreshBake();
    }

    private void Publish()
    {
        WaterRect = ComputeWaterRect();
        float radius = Mathf.Clamp(m_CornerRadius, 0f, Mathf.Min(WaterRect.width, WaterRect.height) * 0.5f);
        Vector2 sun = m_SunDirection.sqrMagnitude > 1e-6f ? m_SunDirection.normalized : Vector2.down;

        Shader.SetGlobalVector(PoolRectId, new Vector4(WaterRect.center.x, WaterRect.center.y, WaterRect.width * 0.5f, WaterRect.height * 0.5f));
        Shader.SetGlobalVector(PoolShapeId, new Vector4(radius, 0f, 0f, 0f));
        Shader.SetGlobalVector(PoolSunId, new Vector4(sun.x, sun.y, m_RimShadowLength, Mathf.Max(0.001f, m_RimShadowSoftness)));
    }

#region Quality

    private void ApplyQuality()
    {
        Profile = ResolveProfile();
        if (Profile.CheapWaterShaders) Shader.EnableKeyword(LowKeyword);
        else Shader.DisableKeyword(LowKeyword);

        // Only touch post-processing in play mode, and only on this scene's own profile instance.
        if (!Application.isPlaying) return;
        if (_bloom == null)
        {
            var volume = FindAnyObjectByType<Volume>();
            if (volume != null && volume.profile.TryGet(out _bloom))
                _bloomWasActive = _bloom.active;
        }
        if (_bloom != null) _bloom.active = _bloomWasActive && Profile.Bloom;
    }

    private GraphicsQualityProfile ResolveProfile()
    {
        switch (m_QualitySource)
        {
            case QualitySource.ForceLow:  return GraphicsQualityProfile.For(GraphicsQuality.Low);
            case QualitySource.ForceMid:  return GraphicsQualityProfile.For(GraphicsQuality.Mid);
            case QualitySource.ForceHigh: return GraphicsQualityProfile.For(GraphicsQuality.High);
        }
        // Edit mode always previews the full look. In play mode without Bootstrap (the Game scene
        // played directly) there is no settings service, so fall back to the device default.
        if (!Application.isPlaying) return GraphicsQualityProfile.For(GraphicsQuality.High);
        return _settings != null
            ? _settings.GraphicsProfile
            : GraphicsQualityProfile.For(GraphicsQualityProfile.DefaultForDevice());
    }

#endregion

#region Floor bake

    private void RefreshBake()
    {
        bool wantBake = Application.isPlaying && m_BakeFloor && m_FloorMaterial != null;
        if (!wantBake)
        {
            ReleaseBake();
            return;
        }

        // Rebake when anything the static layer depends on changes, or the GPU dropped the texture.
        int hash = BakeHash();
        if (hash == _bakedHash && _bakedFloor != null && _bakedFloor.IsCreated()) return;

        int pass = m_FloorMaterial.FindPass(BakePassName);
        if (pass < 0)
        {
            Debug.LogError($"[PoolEnvironment] '{m_FloorMaterial.shader.name}' has no {BakePassName} pass; floor stays live.");
            m_BakeFloor = false;
            return;
        }

        Rect r = WaterRect;
        r.xMin -= m_BakeMargin; r.yMin -= m_BakeMargin;
        r.xMax += m_BakeMargin; r.yMax += m_BakeMargin;
        int ppu = Mathf.Max(8, Profile.FloorBakePixelsPerUnit);
        int max = SystemInfo.maxTextureSize;
        int width = Mathf.Clamp(Mathf.CeilToInt(r.width * ppu), 16, max);
        int height = Mathf.Clamp(Mathf.CeilToInt(r.height * ppu), 16, max);

        if (_bakedFloor == null || _bakedFloor.width != width || _bakedFloor.height != height)
        {
            ReleaseBake();
            // sRGB in a linear project, so 8 bits per channel don't band in the dark blues.
            GraphicsFormat format = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? GraphicsFormat.R8G8B8A8_SRGB
                : GraphicsFormat.R8G8B8A8_UNorm;
            _bakedFloor = new RenderTexture(width, height, GraphicsFormat.None, GraphicsFormat.None)
            {
                graphicsFormat = format,
                name = "PoolBakedFloor",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
            };
            _bakedFloor.Create();
        }

        Shader.SetGlobalVector(PoolBakedRectId, new Vector4(r.xMin, r.yMin, 1f / r.width, 1f / r.height));

        var cmd = new CommandBuffer { name = "Bake Pool Floor" };
        cmd.SetRenderTarget(_bakedFloor);
        cmd.DrawProcedural(Matrix4x4.identity, m_FloorMaterial, pass, MeshTopology.Triangles, 3);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();

        Shader.SetGlobalTexture(PoolBakedFloorId, _bakedFloor);
        Shader.EnableKeyword(BakedKeyword);
        _bakedHash = hash;
    }

    private void ReleaseBake()
    {
        Shader.DisableKeyword(BakedKeyword);
        _bakedHash = 0;
        if (_bakedFloor == null) return;
        _bakedFloor.Release();
        if (Application.isPlaying) Destroy(_bakedFloor);
        else DestroyImmediate(_bakedFloor);
        _bakedFloor = null;
    }

    private int BakeHash()
    {
        int h = WaterRect.GetHashCode();
        h = h * 31 + m_CornerRadius.GetHashCode();
        h = h * 31 + m_SunDirection.GetHashCode();
        h = h * 31 + m_RimShadowLength.GetHashCode();
        h = h * 31 + m_RimShadowSoftness.GetHashCode();
        h = h * 31 + Profile.FloorBakePixelsPerUnit;
        h = h * 31 + m_BakeMargin.GetHashCode();
#if UNITY_EDITOR
        // Lets you tune the floor material in play mode and see it straight away.
        h = h * 31 + (int)m_FloorMaterial.ComputeCRC();
#endif
        return h == 0 ? 1 : h;
    }

#endregion

    private Rect ComputeWaterRect()
    {
        if (m_LeftWall == null || m_RightWall == null || m_TopWall == null || m_BottomWall == null)
            return m_ManualRect;

        float xMin = WorldBounds(m_LeftWall).max.x;
        float xMax = WorldBounds(m_RightWall).min.x;
        float yMin = WorldBounds(m_BottomWall).max.y;
        float yMax = WorldBounds(m_TopWall).min.y;
        if (xMax <= xMin || yMax <= yMin)
        {
            Debug.LogWarning("[PoolEnvironment] Wall colliders don't enclose an area; using the manual rect.");
            return m_ManualRect;
        }
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    // Collider2D.bounds is only valid while physics runs, so derive it from the box itself.
    private static Bounds WorldBounds(BoxCollider2D box)
    {
        Transform t = box.transform;
        Vector2 h = box.size * 0.5f;
        var b = new Bounds(t.TransformPoint(box.offset), Vector3.zero);
        b.Encapsulate(t.TransformPoint(box.offset + new Vector2(-h.x, -h.y)));
        b.Encapsulate(t.TransformPoint(box.offset + new Vector2(-h.x,  h.y)));
        b.Encapsulate(t.TransformPoint(box.offset + new Vector2( h.x, -h.y)));
        b.Encapsulate(t.TransformPoint(box.offset + new Vector2( h.x,  h.y)));
        return b;
    }
}
