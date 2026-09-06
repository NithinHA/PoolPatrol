using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    /// <summary>
    /// Arena-completion bar. A single fillAmount Image spans the arena's total cooldown time; a
    /// <see cref="WaveMarker"/> sits at each wave checkpoint. <see cref="ArenaDirector"/> drives it
    /// directly: the fill moves linearly during cooldown sections and holds still during a wave
    /// (nothing to animate — the wave's marker pulsates instead), then resumes from the same spot
    /// once that wave's marker is marked cleared.
    /// </summary>
    public class ArenaProgressBar : MonoBehaviour
    {
        [SerializeField] private Image m_FillImage;
        [SerializeField] private RectTransform m_MarkerContainer;
        [SerializeField] private WaveMarker m_MarkerPrefab;
        [SerializeField] private float m_FillAnimDuration = 0.4f;

        private readonly List<WaveMarker> _markers = new();
        private Tween _fillTween;

        private void Awake()
        {
            if (m_FillImage == null)
                m_FillImage = GetComponentInChildren<Image>();
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
        public void SetMarkerCleared(int index) => GetMarker(index)?.SetCleared();

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
                _fillTween = m_FillImage.DOFillAmount(value01, m_FillAnimDuration).SetEase(Ease.OutQuad);
            else
                m_FillImage.fillAmount = value01;
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
            ClearMarkers();
        }
    }
}
