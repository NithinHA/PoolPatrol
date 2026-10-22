using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Shop
{
    /// <summary>
    /// The Goddess NPC drawn inside the shop screen (the goddess_anim object). Thin wrapper over its
    /// <see cref="Animator"/> so <see cref="AbilityShopUI"/> can say "arrive", "grant", "leave"
    /// without knowing state or trigger names.
    ///
    /// The animator is switched to unscaled time on Awake: the shop freezes the game through
    /// <c>Time.timeScale</c>, and a Normal-mode animator would never leave its first frame.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class GoddessShopkeeper : MonoBehaviour
    {
        [Header("States")]
        [Tooltip("Played from the first frame every time the shop opens.")]
        [SerializeField] private string m_EnterState = "entry_anim";

        [Tooltip("The leave clip. Close waits for it to finish before hiding the shop.")]
        [SerializeField] private string m_ExitState = "exit_anim";

        [Header("Triggers")]
        [SerializeField] private string m_GrantTrigger = "ability_grant";
        [SerializeField] private string m_ExitTrigger = "exit";

        [Tooltip("Optional. Fired on refresh once the controller has a trigger with this name.")]
        [SerializeField] private string m_RefreshTrigger = "";

        [Tooltip("Upper bound on waiting for the exit clip, in case the controller never reaches it.")]
        [SerializeField] private float m_ExitTimeout = 2.5f;

        private readonly HashSet<string> _triggers = new();

        private Animator _animator;
        private Coroutine _exitRoutine;
        private bool _initialised;

        private void Awake() => EnsureInitialised();

        private void OnDisable() => StopExit();

        private void EnsureInitialised()
        {
            if (_initialised)
                return;
            _initialised = true;

            _animator = GetComponent<Animator>();
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                    _triggers.Add(parameter.name);
            }
        }

        public void PlayEnter()
        {
            EnsureInitialised();
            StopExit();
            ResetTriggers();
            _animator.Play(m_EnterState, 0, 0f);
        }

        public void PlayGrant() => Fire(m_GrantTrigger);

        public void PlayRefresh() => Fire(m_RefreshTrigger);

        /// <summary>Starts the leave clip and calls back once it has played out (or timed out).</summary>
        public void PlayExit(Action onFinished)
        {
            EnsureInitialised();
            StopExit();

            if (!isActiveAndEnabled || !_triggers.Contains(m_ExitTrigger))
            {
                onFinished?.Invoke();
                return;
            }

            // A grant queued right before closing would otherwise play after the exit.
            if (_triggers.Contains(m_GrantTrigger))
                _animator.ResetTrigger(m_GrantTrigger);

            _animator.SetTrigger(m_ExitTrigger);
            _exitRoutine = StartCoroutine(WaitForExit(onFinished));
        }

        /// <summary>Abandons a pending exit without calling back. For force-close paths.</summary>
        public void StopExit()
        {
            if (_exitRoutine == null)
                return;

            StopCoroutine(_exitRoutine);
            _exitRoutine = null;
        }

        private IEnumerator WaitForExit(Action onFinished)
        {
            int exitHash = Animator.StringToHash(m_ExitState);
            float elapsed = 0f;

            while (elapsed < m_ExitTimeout)
            {
                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
                if (state.shortNameHash == exitHash && state.normalizedTime >= 1f && !_animator.IsInTransition(0))
                    break;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _exitRoutine = null;
            onFinished?.Invoke();
        }

        private void Fire(string trigger)
        {
            EnsureInitialised();
            if (!string.IsNullOrEmpty(trigger) && _triggers.Contains(trigger))
                _animator.SetTrigger(trigger);
        }

        private void ResetTriggers()
        {
            foreach (string trigger in _triggers)
                _animator.ResetTrigger(trigger);
        }
    }
}
