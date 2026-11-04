using System;
using System.Collections.Generic;
using Loadout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// A reusable loadout category selector (character / floatie / weapon). Fills a horizontal scroll
    /// row with one thumbnail per unlocked option, highlights the selected one, and moves the selection
    /// with the left/right arrows. Locked options aren't shown at all; they live in the Shop. Indices are
    /// always absolute into the full option list, so the screen and service never see the filtering.
    /// Fires <see cref="OnChanged"/> with the selected absolute index.
    /// </summary>
    public class LoadoutSelectorUI : MonoBehaviour
    {
        [SerializeField] private Button m_LeftButton;
        [SerializeField] private Button m_RightButton;
        [Tooltip("Parent that holds the thumbnail items (the ScrollRect Content).")]
        [SerializeField] private Transform m_Content;
        [SerializeField] private LoadoutSelectionItem m_ItemPrefab;
        [Tooltip("Optional: scrolls the selected item into view.")]
        [SerializeField] private ScrollRect m_ScrollRect;
        [SerializeField] private TMP_Text m_LabelText;

        private readonly List<LoadoutSelectionItem> _items = new();
        private readonly List<int> _visible = new();   // absolute option index of each shown thumbnail
        private IReadOnlyList<LoadoutItemSO> _options;
        private int _position;                         // index into _visible

        /// <summary>Fired with the selected absolute index after it changes.</summary>
        public event Action<int> OnChanged;

        public int CurrentIndex => _visible.Count > 0 ? _visible[_position] : 0;

        private void Awake()
        {
            if (m_LeftButton != null) m_LeftButton.onClick.AddListener(Previous);
            if (m_RightButton != null) m_RightButton.onClick.AddListener(Next);
        }

        public void Populate(IReadOnlyList<LoadoutItemSO> options, Func<int, bool> isUnlocked, int initialIndex)
        {
            _options = options ?? new List<LoadoutItemSO>();

            _visible.Clear();
            for (int i = 0; i < _options.Count; i++)
                if (isUnlocked == null || isUnlocked(i))
                    _visible.Add(i);

            _position = Mathf.Max(0, _visible.IndexOf(initialIndex));

            // Rebuild the row: clear whatever placeholder items are in the scene, then one per option.
            _items.Clear();
            for (int i = 0; i < m_Content.childCount; i++)
                Destroy(m_Content.GetChild(i).gameObject);

            for (int i = 0; i < _visible.Count; i++)
                _items.Add(Instantiate(m_ItemPrefab, m_Content));

            RefreshItems();
            RefreshArrows();
            UpdateLabel();
            ScrollToCurrent();
        }

        public void Next() => SetPosition(_position + 1);
        public void Previous() => SetPosition(_position - 1);

        private void SetPosition(int position)
        {
            if (position == _position || position < 0 || position >= _visible.Count) return;
            _position = position;
            RefreshItems();
            RefreshArrows();
            UpdateLabel();
            ScrollToCurrent();
            OnChanged?.Invoke(_visible[_position]);
        }

        private void RefreshItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                int captured = i;
                _items[i].Set(_options[_visible[i]].Icon, i == _position, () => SetPosition(captured));
            }
        }

        private void RefreshArrows()
        {
            if (m_LeftButton != null) m_LeftButton.interactable = _position > 0;
            if (m_RightButton != null) m_RightButton.interactable = _position < _visible.Count - 1;
        }

        private void UpdateLabel()
        {
            if (m_LabelText != null && _visible.Count > 0)
                m_LabelText.text = _options[_visible[_position]].DisplayName;
        }

        private void ScrollToCurrent()
        {
            if (m_ScrollRect == null || _visible.Count <= 1) return;
            m_ScrollRect.horizontalNormalizedPosition = _position / (float)(_visible.Count - 1);
        }
    }
}
