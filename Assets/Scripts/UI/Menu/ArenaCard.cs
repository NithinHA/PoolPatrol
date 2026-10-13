using System;
using DG.Tweening;
using SpawningLogic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// One arena card in the <see cref="ArenaCarousel"/>. Shows the arena's name, description, and
    /// preview; greys out and locks when the arena isn't unlocked; and grows + shows its highlight
    /// frame while it is the centered (selected) card.
    /// </summary>
    public class ArenaCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_NameText;
        [SerializeField] private TMP_Text m_DescText;
        [SerializeField] private Image m_Icon;
        [Tooltip("Outline/frame shown only while this is the centered card.")]
        [SerializeField] private GameObject m_Highlight;
        [Tooltip("Overlay shown when the arena is locked.")]
        [SerializeField] private GameObject m_Locked;
        [SerializeField] private Button m_Button;

        [Header("Centered animation")]
        [SerializeField] private float m_CenteredScale = 1f;
        [SerializeField] private float m_SideScale = 0.8f;
        [SerializeField] private float m_ScaleTweenDuration = 0.28f;

        private Tween _scaleTween;
        public bool Unlocked { get; private set; }

        public void Init(ArenaDefinitionSO arena, bool unlocked, Action onClicked)
        {
            Unlocked = unlocked;

            if (m_NameText != null) m_NameText.text = arena.ArenaName;
            if (m_DescText != null) m_DescText.text = arena.Description;
            if (m_Icon != null && arena.CardPreview != null) m_Icon.sprite = arena.CardPreview;
            if (m_Locked != null) m_Locked.SetActive(!unlocked);

            if (m_Button != null)
            {
                m_Button.onClick.RemoveAllListeners();
                m_Button.interactable = unlocked;
                if (unlocked && onClicked != null)
                    m_Button.onClick.AddListener(() => onClicked());
            }
        }

        /// <summary>Centered cards grow and show their highlight; side cards shrink and dim it.</summary>
        public void SetCentered(bool centered, bool instant = false)
        {
            if (m_Highlight != null) m_Highlight.SetActive(centered && Unlocked);

            float target = centered ? m_CenteredScale : m_SideScale;
            _scaleTween?.Kill();

            if (instant)
            {
                transform.localScale = Vector3.one * target;
                return;
            }

            _scaleTween = transform.DOScale(target, m_ScaleTweenDuration).SetEase(Ease.OutBack);
        }

        private void OnDestroy() => _scaleTween?.Kill();
    }
}
