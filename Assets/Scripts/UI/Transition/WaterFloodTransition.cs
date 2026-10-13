using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Transition
{
    /// <summary>
    /// Shader-driven water transition. Covering floods the screen from both side edges behind a white
    /// foam front, leaving a blue gradient with drifting foam and faint caustics; revealing opens a
    /// radial wipe that takes the water away.
    ///
    /// All the shaping lives in PoolPatrol/WaterFlood, so this only has to drive four float properties
    /// and keep the shader told about the aspect ratio. Everything runs on unscaled time so a paused or
    /// mid-load game cannot stall the transition.
    /// </summary>
    public class WaterFloodTransition : ScreenTransitionEffect
    {
        private static readonly int FloodId = Shader.PropertyToID("_Flood");
        private static readonly int WipeId = Shader.PropertyToID("_Wipe");
        private static readonly int FoamId = Shader.PropertyToID("_FoamAmount");
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");

        [Tooltip("The object toggled on/off for the whole effect (usually this GameObject).")]
        [SerializeField] private GameObject m_Root;
        [Tooltip("Full-screen graphic carrying the WaterFlood material.")]
        [SerializeField] private RawImage m_Surface;

        [Header("Timing")]
        [SerializeField] private float m_CoverDuration = 0.7f;
        [SerializeField] private float m_RevealDuration = 0.75f;
        [Tooltip("How long the surface foam takes to dissipate once the screen is covered.")]
        [SerializeField] private float m_FoamSettleDuration = 0.45f;
        [Tooltip("Flood easing. Ease out reads as water thrown in hard that then spreads and settles.")]
        [SerializeField] private Ease m_CoverEase = Ease.OutSine;
        [SerializeField] private Ease m_RevealEase = Ease.InOutSine;

        private Material _material;
        private Sequence _sequence;
        private Vector2 _wipeCenter = new Vector2(0.5f, 0.5f);

        private void Awake()
        {
            // Instance the material so tweening never writes back into the shared asset.
            _material = new Material(m_Surface.material);
            m_Surface.material = _material;
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            if (_material != null) Destroy(_material);
        }

        public override void Cover(Vector2 screenOrigin, Action onComplete)
        {
            Begin();

            // The reveal opens from wherever the cover was triggered, so the whole transition reads as
            // one gesture centred on the button the player actually pressed.
            _wipeCenter = new Vector2(
                Mathf.Clamp01(screenOrigin.x / Mathf.Max(Screen.width, 1)),
                Mathf.Clamp01(screenOrigin.y / Mathf.Max(Screen.height, 1)));

            _material.SetFloat(FloodId, 0f);
            _material.SetFloat(WipeId, 0f);
            _material.SetFloat(FoamId, 1f);

            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(_material.DOFloat(1f, FloodId, m_CoverDuration).SetEase(m_CoverEase));
            _sequence.AppendCallback(() => onComplete?.Invoke());
            // Patches linger a moment, then erode away while the next scene loads behind the water.
            _sequence.Append(_material.DOFloat(0f, FoamId, m_FoamSettleDuration).SetEase(Ease.InQuad));
        }

        public override void Reveal(Action onComplete = null)
        {
            Begin();

            _material.SetFloat(FloodId, 1f);
            _material.SetFloat(WipeId, 0f);
            _material.SetVector(CenterId, new Vector4(_wipeCenter.x, _wipeCenter.y, 0f, 0f));

            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(_material.DOFloat(0f, FoamId, 0.2f));
            _sequence.Join(_material.DOFloat(1f, WipeId, m_RevealDuration).SetEase(m_RevealEase));
            _sequence.OnComplete(() =>
            {
                if (m_Root != null) m_Root.SetActive(false);
                onComplete?.Invoke();
            });
        }

        /// <summary>Shows the overlay and re-syncs the aspect, which can change between transitions.</summary>
        private void Begin()
        {
            _sequence?.Kill();
            if (m_Root != null) m_Root.SetActive(true);
            _material.SetFloat(AspectId, Screen.width / (float)Mathf.Max(Screen.height, 1));
        }
    }
}
