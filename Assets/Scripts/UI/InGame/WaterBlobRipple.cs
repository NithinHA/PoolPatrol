using UnityEngine;
using UnityEngine.UI;

namespace UI.InGame
{
    /// <summary>
    /// Drives the <c>PoolPatrol/UIWaterBlob</c> shader on the pause menu's water-blob background.
    ///
    /// The shader cannot use Unity's built-in <c>_Time</c>: the pause menu is on screen precisely
    /// when <c>Time.timeScale</c> is 0, which freezes it. So this pushes an unscaled clock into
    /// <c>_UnscaledTime</c> instead. Works on a private material instance, never the shared asset,
    /// and only ticks while the object is enabled so a closed menu costs nothing.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class WaterBlobRipple : MonoBehaviour
    {
        [Tooltip("UV distortion amount. Keep small — this warps the sprite, so large values smear it.")]
        [Range(0f, 0.03f)]
        [SerializeField] private float m_RippleStrength = 0.006f;

        [Tooltip("How many wave crests run across the blob.")]
        [Range(0.5f, 12f)]
        [SerializeField] private float m_RippleFrequency = 4.5f;

        [Tooltip("Ripple travel speed.")]
        [Range(0f, 4f)]
        [SerializeField] private float m_RippleSpeed = 0.9f;

        [Tooltip("Strength of the slow light sheen sliding across the blob. 0 disables it.")]
        [Range(0f, 0.4f)]
        [SerializeField] private float m_SheenStrength = 0.12f;

        private static readonly int TimeId      = Shader.PropertyToID("_UnscaledTime");
        private static readonly int StrengthId  = Shader.PropertyToID("_RippleStrength");
        private static readonly int FrequencyId = Shader.PropertyToID("_RippleFrequency");
        private static readonly int SpeedId     = Shader.PropertyToID("_RippleSpeed");
        private static readonly int SheenId     = Shader.PropertyToID("_SheenStrength");
        private static readonly int SpriteRectId = Shader.PropertyToID("_SpriteRect");

        private Material _material;
        private float _time;

        private void Awake()
        {
            Image image = GetComponent<Image>();
            if (image.material == null || !image.material.HasProperty(TimeId))
            {
                // No ripple material assigned — the blob just renders as a plain sprite.
                enabled = false;
                return;
            }

            _material = new Material(image.material);
            image.material = _material;
            ApplySettings();
            ApplySpriteRect(image.sprite);
        }

        private void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                ApplySettings();
        }
#endif

        private void Update()
        {
            if (_material == null)
                return;

            _time += Time.unscaledDeltaTime;
            _material.SetFloat(TimeId, _time);
        }

        /// <summary>
        /// Tells the shader which slice of the texture this sprite occupies. The blob is one sprite
        /// in a multi-sprite sheet, so its mesh UVs are sheet-space; without this the ripple would
        /// be phased against the whole sheet and could sample a neighbouring sprite.
        /// </summary>
        private void ApplySpriteRect(Sprite sprite)
        {
            if (_material == null)
                return;

            if (sprite == null || sprite.texture == null)
            {
                _material.SetVector(SpriteRectId, new Vector4(0f, 0f, 1f, 1f));
                return;
            }

            Rect rect = sprite.textureRect;
            float w = sprite.texture.width;
            float h = sprite.texture.height;

            _material.SetVector(SpriteRectId, new Vector4(
                rect.xMin / w, rect.yMin / h, rect.xMax / w, rect.yMax / h));
        }

        private void ApplySettings()
        {
            if (_material == null)
                return;

            _material.SetFloat(StrengthId, m_RippleStrength);
            _material.SetFloat(FrequencyId, m_RippleFrequency);
            _material.SetFloat(SpeedId, m_RippleSpeed);
            _material.SetFloat(SheenId, m_SheenStrength);
        }
    }
}
