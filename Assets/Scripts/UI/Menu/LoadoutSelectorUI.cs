using System;
using System.Collections.Generic;
using Loadout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// A reusable loadout category selector (character / floatie / weapon / pool texture). Fills a
    /// horizontal scroll row with one thumbnail per option, highlights the selected one, and moves the
    /// selection with the left/right arrows (which skip locked entries). Locked entries still appear,
    /// greyed, as a teaser. Fires <see cref="OnChanged"/> with the selected absolute index.
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
        private IReadOnlyList<LoadoutItemSO> _options;
        private Func<int, bool> _isUnlocked;
        private int _current;

        /// <summary>Fired with the selected absolute index after it changes.</summary>
        public event Action<int> OnChanged;

        public int CurrentIndex => _current;

        private void Awake()
        {
            if (m_LeftButton != null) m_LeftButton.onClick.AddListener(Previous);
            if (m_RightButton != null) m_RightButton.onClick.AddListener(Next);
        }

        public void Populate(IReadOnlyList<LoadoutItemSO> options, Func<int, bool> isUnlocked, int initialIndex)
        {
            _options = options ?? new List<LoadoutItemSO>();
            _isUnlocked = isUnlocked ?? (_ => true);
            _current = Mathf.Clamp(initialIndex, 0, Mathf.Max(0, _options.Count - 1));

            // Rebuild the row: clear whatever placeholder items are in the scene, then one per option.
            foreach (LoadoutSelectionItem item in _items)
                if (item != null) Destroy(item.gameObject);
            _items.Clear();

            for (int i = 0; i < m_Content.childCount; i++)
                Destroy(m_Content.GetChild(i).gameObject);

            for (int i = 0; i < _options.Count; i++)
            {
                LoadoutSelectionItem item = Instantiate(m_ItemPrefab, m_Content);
                _items.Add(item);
            }

            RefreshItems();
            RefreshArrows();
            UpdateLabel();
        }

        public void Next() => MoveToUnlocked(+1);
        public void Previous() => MoveToUnlocked(-1);

        private void MoveToUnlocked(int dir)
        {
            for (int i = _current + dir; i >= 0 && i < _options.Count; i += dir)
            {
                if (_isUnlocked(i)) { SetCurrent(i); return; }
            }
        }

        private void SetCurrent(int index)
        {
            if (index == _current || index < 0 || index >= _options.Count) return;
            _current = index;
            RefreshItems();
            RefreshArrows();
            UpdateLabel();
            ScrollToCurrent();
            OnChanged?.Invoke(_current);
        }

        private void RefreshItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                bool unlocked = _isUnlocked(i);
                int captured = i;
                _items[i].Set(_options[i].Icon, unlocked, i == _current,
                    unlocked ? () => SetCurrent(captured) : (Action)null);
            }
        }

        private void RefreshArrows()
        {
            if (m_LeftButton != null) m_LeftButton.interactable = HasUnlocked(-1);
            if (m_RightButton != null) m_RightButton.interactable = HasUnlocked(+1);
        }

        private bool HasUnlocked(int dir)
        {
            for (int i = _current + dir; i >= 0 && i < _options.Count; i += dir)
                if (_isUnlocked(i)) return true;
            return false;
        }

        private void UpdateLabel()
        {
            if (m_LabelText != null && _current >= 0 && _current < _options.Count)
                m_LabelText.text = _options[_current].DisplayName;
        }

        private void ScrollToCurrent()
        {
            if (m_ScrollRect == null || _options.Count <= 1) return;
            m_ScrollRect.horizontalNormalizedPosition = _current / (float)(_options.Count - 1);
        }
    }
}
