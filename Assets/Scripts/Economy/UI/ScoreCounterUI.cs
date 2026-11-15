using DG.Tweening;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UI.HUD;
using UnityEngine;

namespace Economy.UI
{
    /// <summary>
    /// HUD score pill. Rolls its label up to <see cref="IRunStatsService.Score"/> whenever the run
    /// stats change, and gives the star icon a small punch so a scoring kill registers at a glance.
    /// Only the label is touched while rolling — put a nested Canvas on this object so the roll does
    /// not re-batch the rest of the HUD.
    /// </summary>
    public class ScoreCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_CountText;
        [SerializeField] private RectTransform m_Icon;

        [Header("Animation")]
        [SerializeField] private float m_CountDuration = 0.45f;
        [SerializeField] private float m_IconPunchScale = 0.25f;
        [SerializeField] private float m_IconPunchDuration = 0.3f;

        private IRunStatsService _runStats;
        private int _displayed;
        private int _target;
        private Tween _countTween;
        private Tween _punchTween;

        private void Start()
        {
            _runStats = ServiceLocator.GetRunStatsService();
            if (_runStats == null)
            {
                Debug.LogWarning("[ScoreCounterUI] RunStatsService not available.");
                HudNumberText.SetGrouped(m_CountText, 0);
                return;
            }

            _displayed = _target = _runStats.Score;
            HudNumberText.SetGrouped(m_CountText, _displayed);
            _runStats.OnStatsChanged += OnStatsChanged;
        }

        private void OnDestroy()
        {
            if (_runStats != null)
                _runStats.OnStatsChanged -= OnStatsChanged;
            _countTween?.Kill();
            _punchTween?.Kill();
        }

        // Fires for every stat (kills, waves, streak…) — only react when the score itself moved.
        private void OnStatsChanged()
        {
            int score = _runStats.Score;
            if (score == _target)
                return;

            bool increased = score > _target;
            _target = score;

            _countTween?.Kill();
            if (!increased)
            {
                // A reset (new run) snaps rather than counting down.
                HudNumberText.SetGrouped(m_CountText, _displayed = score);
                return;
            }

            _countTween = DOTween.To(() => _displayed, SetDisplayed, _target, m_CountDuration).SetEase(Ease.OutQuad);
            Punch();
        }

        private void SetDisplayed(int value)
        {
            if (value == _displayed)
                return;
            _displayed = value;
            HudNumberText.SetGrouped(m_CountText, value);
        }

        private void Punch()
        {
            if (m_Icon == null)
                return;

            // Complete (not Kill) so an interrupted punch lands back on scale 1 instead of drifting.
            _punchTween?.Complete();
            _punchTween = m_Icon.DOPunchScale(Vector3.one * m_IconPunchScale, m_IconPunchDuration, 6, 0.6f);
        }
    }
}
