using System;
using DG.Tweening;
using UnityEngine;

namespace Abilities.Goddess
{
    /// <summary>
    /// The mid-run shop vendor in the world (doc §2, §5-§7). She emerges near the centre of the
    /// arena, stays for a fixed availability window, and opens the shop the instant the player
    /// touches her — no hold, no button, no multi-step interaction.
    ///
    /// She is a pure presentation + trigger component: she raises <see cref="OnTouched"/> and lets
    /// <see cref="GoddessEncounterDirector"/> decide what that means. She never damages, blocks or
    /// is damaged; enemies and projectiles pass straight through.
    ///
    /// Put this on a prefab whose root has a trigger collider (the shipped
    /// <c>Assets/Prefabs/AbilitySelection/PoolGoddess.prefab</c> already has one).
    /// </summary>
    [DisallowMultipleComponent]
    public class PoolGoddess : MonoBehaviour
    {
        public enum GoddessState { Hidden, Emerging, Available, Busy, Retreating }

        [Header("Visuals")]
        [Tooltip("Scaled/faded during emerge and retreat. Defaults to this transform's first child.")]
        [SerializeField] private Transform m_Visual;

        [Tooltip("Optional emerge telegraph — ripples, bubbles, sparkles (doc §5).")]
        [SerializeField] private ParticleSystem m_EmergeEffect;

        [Tooltip("Optional retreat effect, played as she sinks back down.")]
        [SerializeField] private ParticleSystem m_RetreatEffect;

        [Header("Timing")]
        [Min(0.05f)] [SerializeField] private float m_EmergeDuration = 0.6f;
        [Min(0.05f)] [SerializeField] private float m_RetreatDuration = 0.45f;

        [Tooltip("How high above her resting position she rises out of the water.")]
        [SerializeField] private float m_EmergeRiseDistance = 0.4f;

        [Header("Interaction")]
        [Tooltip("Trigger collider used for the touch check. Defaults to the collider on this object.")]
        [SerializeField] private Collider2D m_TouchTrigger;

        /// <summary>Raised the instant the player touches her while available.</summary>
        public event Action<PoolGoddess> OnTouched;

        /// <summary>
        /// Raised once she is fully gone, whether because the window expired or because the shop
        /// closed. The director uses this to despawn her and schedule the next visit.
        /// </summary>
        public event Action<PoolGoddess> OnRetreated;

        public GoddessState State { get; private set; } = GoddessState.Hidden;

        /// <summary>Seconds left in the availability window. 0 when she is not available.</summary>
        public float WindowRemaining { get; private set; }

        private Vector3 _restPosition;
        private Sequence _sequence;

        private void Awake()
        {
            if (m_Visual == null && transform.childCount > 0)
                m_Visual = transform.GetChild(0);
            if (m_TouchTrigger == null)
                m_TouchTrigger = GetComponent<Collider2D>();

            if (m_TouchTrigger != null)
                m_TouchTrigger.isTrigger = true;

            SetInteractable(false);
            if (m_Visual != null)
                m_Visual.localScale = Vector3.zero;
        }

        private void Update()
        {
            if (State != GoddessState.Available)
                return;

            // Scaled time on purpose: the window must not drain while the game is paused.
            WindowRemaining -= Time.deltaTime;
            if (WindowRemaining <= 0f)
            {
                WindowRemaining = 0f;
                Retreat();
            }
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            if (m_Visual != null)
                m_Visual.DOKill();
        }

        /// <summary>
        /// Rises out of the water and stays touchable for <paramref name="windowSeconds"/>
        /// (doc §6 — 15s by default). The window only starts once the emerge animation finishes,
        /// so a slow emerge never eats into the player's reaction time.
        /// </summary>
        public void Appear(float windowSeconds)
        {
            if (State != GoddessState.Hidden)
                return;

            State = GoddessState.Emerging;
            WindowRemaining = Mathf.Max(0f, windowSeconds);
            _restPosition = transform.position;

            if (m_EmergeEffect != null)
                m_EmergeEffect.Play();

            PlayTransition(
                from: _restPosition - Vector3.up * m_EmergeRiseDistance,
                to: _restPosition,
                fromScale: 0f,
                toScale: 1f,
                duration: m_EmergeDuration,
                ease: Ease.OutBack,
                onComplete: () =>
                {
                    State = GoddessState.Available;
                    SetInteractable(true);
                });
        }

        /// <summary>
        /// Freezes the availability window while the shop is open, so she cannot expire out from
        /// under a player who is mid-purchase.
        /// </summary>
        public void EnterShop()
        {
            if (State != GoddessState.Available)
                return;

            State = GoddessState.Busy;
            SetInteractable(false);
        }

        /// <summary>Sinks back down and raises <see cref="OnRetreated"/> (doc §6, §21).</summary>
        public void Retreat()
        {
            if (State is GoddessState.Hidden or GoddessState.Retreating)
                return;

            State = GoddessState.Retreating;
            WindowRemaining = 0f;
            SetInteractable(false);

            if (m_RetreatEffect != null)
                m_RetreatEffect.Play();

            PlayTransition(
                from: transform.position,
                to: _restPosition - Vector3.up * m_EmergeRiseDistance,
                fromScale: m_Visual != null ? m_Visual.localScale.x : 1f,
                toScale: 0f,
                duration: m_RetreatDuration,
                ease: Ease.InBack,
                onComplete: () =>
                {
                    State = GoddessState.Hidden;
                    OnRetreated?.Invoke(this);
                });
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (State != GoddessState.Available)
                return;
            if (!other.CompareTag(Constants.GameConstants.TAG_Player))
                return;

            OnTouched?.Invoke(this);
        }

        private void SetInteractable(bool value)
        {
            if (m_TouchTrigger != null)
                m_TouchTrigger.enabled = value;
        }

        private void PlayTransition(Vector3 from, Vector3 to, float fromScale, float toScale,
            float duration, Ease ease, TweenCallback onComplete)
        {
            _sequence?.Kill();
            transform.position = from;

            _sequence = DOTween.Sequence();
            _sequence.Join(transform.DOMove(to, duration).SetEase(ease));

            if (m_Visual != null)
            {
                m_Visual.localScale = Vector3.one * fromScale;
                _sequence.Join(m_Visual.DOScale(toScale, duration).SetEase(ease));
            }

            _sequence.OnComplete(onComplete);
        }
    }
}
