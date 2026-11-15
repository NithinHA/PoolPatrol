using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    /// <summary>
    /// Arena-completion bar. A single fill spans the arena's total cooldown time; a
    /// <see cref="WaveMarker"/> sits at each wave checkpoint. <see cref="ArenaDirector"/> drives it
    /// directly: the fill moves linearly during cooldown sections and holds still during a wave
    /// (nothing to animate — the wave's marker pulsates instead), then resumes from the same spot
    /// once that wave's marker is marked cleared.
    ///
    /// The fill is drawn one of two ways:
    /// <list type="bullet">
    /// <item>With <see cref="m_FillMask"/> set, the fill Image is a full-width sliced capsule and the
    /// RectMask2D's right padding reveals it — so its rounded caps keep their shape at any size.</item>
    /// <item>Without it, <see cref="m_FillImage"/> must be a Filled image and fillAmount is driven.</item>
    /// </list>
    /// </summary>
    public class ArenaProgressBar : MonoBehaviour
    {
        [SerializeField] private Image m_FillImage;
        [Tooltip("Optional. Clips a full-width fill instead of using Image.fillAmount.")]
        [SerializeField] private RectMask2D m_FillMask;
        [SerializeField] private RectTransform m_MarkerContainer;
        [SerializeField] private WaveMarker m_MarkerPrefab;
        [SerializeField] private float m_FillAnimDuration = 0.4f;

        [Header("Checkpoint cleared")]
        [Tooltip("The fill briefly brightens to this colour when a wave marker is cleared.")]
        [SerializeField] private Color m_ClearFlashColor = new Color(1f, 1f, 0.8f, 1f);
        [SerializeField] private float m_ClearFlashDuration = 0.15f;

        private readonly List<WaveMarker> _markers = new();
        private Tween _fillTween;
        private Tween _flashTween;
        private float _progress;
        private Color _fillBaseColor = Color.white;

        private void Awake()
        {
            if (m_FillImage == null)
                m_FillImage = GetComponentInChildren<Image>();
            if (m_FillImage != null)
            {
                _fillBaseColor = m_FillImage.color;
                ApplyProgress(0f);   // Empty until ArenaDirector reports, whatever the scene was saved at.
            }
        }

        /// <summary>Builds one pending marker per wave checkpoint, at its 0..1 position along the bar.</summary>
        public void BuildMarkers(IReadOnlyList<float> positions01)
        {
            ClearMarkers();

            if (m_MarkerPrefab == null || m_MarkerContainer == null || positions01 == null)
                return;

            foreach (float frac in positions01)
            {
                var marker = Instantiate(m_MarkerPrefab, m_MarkerContainer);
                if (marker.TryGetComponent<RectTransform>(out var rt))
                {
                    rt.anchorMin = new Vector2(frac, 0.5f);
                    rt.anchorMax = new Vector2(frac, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                }

                marker.SetPending();
                _markers.Add(marker);
            }
        }

        public void SetMarkerActive(int index) => GetMarker(index)?.SetActivePulsing();

        public void SetMarkerCleared(int index)
        {
            WaveMarker marker = GetMarker(index);
            if (marker == null)
                return;

            marker.SetCleared();
            FlashFill();
        }

        private WaveMarker GetMarker(int index) =>
            (index >= 0 && index < _markers.Count) ? _markers[index] : null;

        /// <summary>Sets the fill instantly, no tween.</summary>
        public void SnapTo(float value01) => SetProgress(value01, animate: false);

        public void SetProgress(float value01, bool animate = true)
        {
            if (m_FillImage == null)
                return;

            value01 = Mathf.Clamp01(value01);

            _fillTween?.Kill();
            if (animate)
                _fillTween = DOTween.To(() => _progress, ApplyProgress, value01, m_FillAnimDuration).SetEase(Ease.OutQuad);
            else
                ApplyProgress(value01);
        }

        private void ApplyProgress(float value01)
        {
            _progress = value01;

            if (m_FillMask != null)
            {
                float width = m_FillMask.rectTransform.rect.width;
                m_FillMask.padding = new Vector4(0f, 0f, width * (1f - value01), 0f);
            }
            else
            {
                m_FillImage.fillAmount = value01;
            }
        }

        /// <summary>A quick brighten-and-back on the fill so clearing a wave is felt along the bar.</summary>
        private void FlashFill()
        {
            if (m_FillImage == null || m_ClearFlashDuration <= 0f)
                return;

            _flashTween?.Kill();
            m_FillImage.color = _fillBaseColor;
            _flashTween = m_FillImage.DOColor(m_ClearFlashColor, m_ClearFlashDuration)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .OnKill(() =>
                {
                    if (m_FillImage != null)
                        m_FillImage.color = _fillBaseColor;
                });
        }

        private void ClearMarkers()
        {
            foreach (var marker in _markers)
                if (marker != null)
                    Destroy(marker.gameObject);
            _markers.Clear();
        }

        private void OnDestroy()
        {
            _fillTween?.Kill();
            _flashTween?.Kill();
            ClearMarkers();
        }
    }
}
