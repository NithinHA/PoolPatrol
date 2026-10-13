using System;
using System.Collections.Generic;
using DG.Tweening;
using PTL.Framework;
using PTL.Framework.Services;
using SpawningLogic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// Center-snapping arena carousel. Lays every arena card out in a horizontal row inside a masked
    /// viewport, centers one at a time (centered card enlarged + highlighted via <see cref="ArenaCard"/>),
    /// and supports both arrow buttons and swipe-with-snap. Reports the centered arena index.
    /// </summary>
    public class ArenaCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private ArenaCard m_CardPrefab;
        [Tooltip("The row the cards are parented to; this is what slides. Anchored at the viewport centre.")]
        [SerializeField] private RectTransform m_Container;
        [Tooltip("Horizontal distance between card centres, in canvas units.")]
        [SerializeField] private float m_CardSpacing = 620f;

        [SerializeField] private Button m_LeftButton;
        [SerializeField] private Button m_RightButton;

        [Header("Snap")]
        [SerializeField] private float m_SnapDuration = 0.35f;
        [SerializeField] private Ease m_SnapEase = Ease.OutCubic;

        private readonly List<ArenaCard> _cards = new();
        private int _selectedIndex;
        private Tween _slideTween;
        private Canvas _canvas;

        /// <summary>Fired whenever the centered arena changes (arrows, swipe-snap, or programmatic).</summary>
        public event Action<int> OnSelectionChanged;

        public int SelectedIndex => _selectedIndex;
        public int Count => _cards.Count;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (m_LeftButton != null) m_LeftButton.onClick.AddListener(Previous);
            if (m_RightButton != null) m_RightButton.onClick.AddListener(Next);
        }

        private void OnDestroy() => _slideTween?.Kill();

        /// <summary>Rebuilds the card row from progression and centres on <paramref name="initialIndex"/>.</summary>
        public void Build(int initialIndex)
        {
            foreach (ArenaCard card in _cards)
                if (card != null) Destroy(card.gameObject);
            _cards.Clear();

            // Also clear any designer-placed placeholder cards left in the container.
            for (int i = m_Container.childCount - 1; i >= 0; i--)
                Destroy(m_Container.GetChild(i).gameObject);

            IProgression progression = ServiceLocator.GetProgressionService();
            if (progression == null) return;

            for (int i = 0; i < progression.ArenaCount; i++)
            {
                ArenaDefinitionSO arena = progression.GetArenaDefinition(i);
                bool unlocked = progression.IsLevelUnlocked(i, 0);

                ArenaCard card = Instantiate(m_CardPrefab, m_Container);
                var rt = (RectTransform)card.transform;
                rt.anchoredPosition = new Vector2(i * m_CardSpacing, 0f);

                int index = i;
                card.Init(arena, unlocked, () => CenterOrConfirm(index));
                _cards.Add(card);
            }

            _selectedIndex = Mathf.Clamp(initialIndex, 0, Mathf.Max(0, _cards.Count - 1));
            SnapTo(_selectedIndex, instant: true);
            RefreshArrows();
        }

        public void Next() => SnapTo(_selectedIndex + 1);
        public void Previous() => SnapTo(_selectedIndex - 1);

        private void CenterOrConfirm(int index)
        {
            // Tapping a side card centres it; tapping the already-centred card is handled by the screen.
            if (index != _selectedIndex) SnapTo(index);
        }

        private void SnapTo(int index, bool instant = false)
        {
            index = Mathf.Clamp(index, 0, Mathf.Max(0, _cards.Count - 1));
            bool changed = index != _selectedIndex;
            _selectedIndex = index;

            float targetX = -index * m_CardSpacing;
            _slideTween?.Kill();
            if (instant)
                m_Container.anchoredPosition = new Vector2(targetX, m_Container.anchoredPosition.y);
            else
                _slideTween = m_Container.DOAnchorPosX(targetX, m_SnapDuration).SetEase(m_SnapEase);

            UpdateCenteredStates(instant);
            RefreshArrows();
            if (changed || instant) OnSelectionChanged?.Invoke(_selectedIndex);
        }

        private void UpdateCenteredStates(bool instant)
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].SetCentered(i == _selectedIndex, instant);
        }

        private void RefreshArrows()
        {
            if (m_LeftButton != null) m_LeftButton.interactable = _selectedIndex > 0;
            if (m_RightButton != null) m_RightButton.interactable = _selectedIndex < _cards.Count - 1;
        }

#region Swipe

        public void OnBeginDrag(PointerEventData eventData) => _slideTween?.Kill();

        public void OnDrag(PointerEventData eventData)
        {
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            float x = m_Container.anchoredPosition.x + eventData.delta.x / scale;

            // Soft clamp so the row can't be dragged far past either end.
            float min = -(_cards.Count - 1) * m_CardSpacing - m_CardSpacing * 0.4f;
            float max = m_CardSpacing * 0.4f;
            m_Container.anchoredPosition = new Vector2(Mathf.Clamp(x, min, max), m_Container.anchoredPosition.y);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            int nearest = Mathf.RoundToInt(-m_Container.anchoredPosition.x / m_CardSpacing);
            SnapTo(nearest);
        }

#endregion
    }
}
