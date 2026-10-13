using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Drives the blur amount on the menu background Image. Works on a private material instance so the
    /// shared material asset is never edited. The navigator tweens the blur to 0 when the loadout screen
    /// is shown (so the avatar appears to float on the pool) and back to the default otherwise.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class MenuBackgroundBlur : MonoBehaviour
    {
        [Tooltip("Blur amount shown on the menu/arena/level screens.")]
        [SerializeField] private float m_DefaultBlur = 6f;
        [SerializeField] private float m_TweenDuration = 0.5f;

        private static readonly int BlurSizeId = Shader.PropertyToID("_BlurSize");

        private Material _material;
        private float _current;
        private Tween _tween;

        public float DefaultBlur => m_DefaultBlur;

        private void Awake()
        {
            Image image = GetComponent<Image>();
            if (image.material != null && image.material.HasProperty(BlurSizeId))
            {
                // Clone so we animate an instance, never the shared asset.
                _material = new Material(image.material);
                image.material = _material;
            }

            SetBlur(m_DefaultBlur);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            if (_material != null) Destroy(_material);
        }

        public void SetBlur(float value)
        {
            _current = value;
            if (_material != null) _material.SetFloat(BlurSizeId, value);
        }

        public void TweenTo(float target, float duration = -1f)
        {
            if (duration < 0f) duration = m_TweenDuration;
            _tween?.Kill();

            if (_material == null)
            {
                _current = target;
                return;
            }

            _tween = DOVirtual.Float(_current, target, duration, SetBlur).SetEase(Ease.InOutSine).SetUpdate(true);
        }

        public void TweenToDefault() => TweenTo(m_DefaultBlur);
        public void TweenToZero() => TweenTo(0f);
    }
}
