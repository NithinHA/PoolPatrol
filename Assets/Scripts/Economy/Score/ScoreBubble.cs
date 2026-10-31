using System;
using DG.Tweening;
using Pooling;
using TMPro;
using UnityEngine;

namespace Economy
{
    /// <summary>
    /// World-space popup showing the score one shot (or one gem pickup) earned: the final integer
    /// only, no breakdown and no "+". Floats upward and fades, then returns itself to the pool.
    /// Colour and size step up by tier, so a big multi-kill reads differently from a lone kill.
    ///
    /// Spawned by <see cref="ScoreKeeper"/> through <see cref="ObjectPoolManager"/>
    /// (<see cref="PoolableItemType.ScoreBubble"/>); call <see cref="Show"/> right after spawning.
    /// </summary>
    public class ScoreBubble : MonoBehaviour, IPoolableObject
    {
        [Serializable]
        private struct Tier
        {
            [Tooltip("Lowest score that uses this tier.")]
            public int MinScore;
            public Color Color;
            [Tooltip("Scale multiplier — grows slightly with tier.")]
            public float Scale;
        }

        [SerializeField] private TMP_Text m_Text;

        [Header("Tiers (ascending by Min Score)")]
        [SerializeField] private Tier[] m_Tiers =
        {
            new() { MinScore = 0,   Color = Color.white,                   Scale = 1f    },
            new() { MinScore = 40,  Color = new Color(1f, 0.85f, 0.2f),    Scale = 1.15f },
            new() { MinScore = 100, Color = new Color(1f, 0.4f, 0.25f),    Scale = 1.3f  },
        };

        [Header("Animation")]
        [SerializeField] private float m_Duration = 0.5f;
        [Tooltip("World units the bubble rises over its lifetime.")]
        [SerializeField] private float m_RiseDistance = 0.8f;
        [Tooltip("Fraction of the duration it stays fully opaque before fading.")]
        [Range(0f, 1f)] [SerializeField] private float m_HoldFraction = 0.4f;
        [Tooltip("Pop-in: scale starts at this fraction of the tier scale.")]
        [Range(0f, 1f)] [SerializeField] private float m_PopFrom = 0.6f;

        [Header("Keep clear of the HUD (viewport 0..1)")]
        [Tooltip("Min / max viewport x the bubble's spawn point is clamped into.")]
        [SerializeField] private Vector2 m_ViewportX = new(0.1f, 0.85f);
        [Tooltip("Min / max viewport y. The top is lower so the rise never runs into the top HUD.")]
        [SerializeField] private Vector2 m_ViewportY = new(0.08f, 0.8f);

        private PoolableItemType _itemType;
        private Sequence _sequence;
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
            if (m_Text == null)
                m_Text = GetComponentInChildren<TMP_Text>();
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        public void Show(int score)
        {
            Tier tier = TierFor(score);

            transform.position = KeepOnScreen(transform.position);
            transform.localScale = Vector3.one * (tier.Scale * m_PopFrom);

            m_Text.text = score.ToString();
            Color color = tier.Color;
            color.a = 1f;
            m_Text.color = color;

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Join(transform.DOMoveY(transform.position.y + m_RiseDistance, m_Duration).SetEase(Ease.OutCubic))
                .Join(transform.DOScale(Vector3.one * tier.Scale, m_Duration * 0.3f).SetEase(Ease.OutBack))
                .Insert(m_Duration * m_HoldFraction, m_Text.DOFade(0f, m_Duration * (1f - m_HoldFraction)).SetEase(Ease.InQuad))
                .OnComplete(ReturnToPool);
        }

        private Tier TierFor(int score)
        {
            Tier best = m_Tiers is { Length: > 0 } ? m_Tiers[0] : new Tier { Color = Color.white, Scale = 1f };
            if (m_Tiers == null)
                return best;

            foreach (Tier tier in m_Tiers)
                if (score >= tier.MinScore && tier.MinScore >= best.MinScore)
                    best = tier;
            return best;
        }

        /// <summary>Nudges the spawn point inside the safe viewport rect, so it neither clips nor sits under the HUD.</summary>
        private Vector3 KeepOnScreen(Vector3 world)
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return world;

            Vector3 viewport = _camera.WorldToViewportPoint(world);
            viewport.x = Mathf.Clamp(viewport.x, m_ViewportX.x, m_ViewportX.y);
            viewport.y = Mathf.Clamp(viewport.y, m_ViewportY.x, m_ViewportY.y);

            Vector3 clamped = _camera.ViewportToWorldPoint(viewport);
            clamped.z = world.z;
            return clamped;
        }

#region IPoolableObject

        public void Initialize(PoolableItemType type)
        {
            _itemType = type;
        }

        /// <summary>All per-show state is set by <see cref="Show"/>.</summary>
        public void Reset() { }

        public void ReturnToPool()
        {
            _sequence = null;
            ObjectPoolManager.Instance.ReleaseItem(_itemType, this);
        }

#endregion
    }
}
