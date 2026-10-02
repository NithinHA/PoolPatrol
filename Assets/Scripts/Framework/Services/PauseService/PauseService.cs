using System;
using System.Collections.Generic;
using UnityEngine;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Default <see cref="IPauseService"/>. Holds a set of pause sources and mirrors "the set is
    /// non-empty" onto <c>Time.timeScale</c>.
    ///
    /// Because the pause is a timescale freeze, anything driven by <c>Time.deltaTime</c>,
    /// <c>FixedUpdate</c> or a scaled <c>WaitForSeconds</c> stops for free — enemies, bullets,
    /// the arena director's coroutines and DOTween tweens included. UI still runs, since uGUI and
    /// unscaled-time tweens are unaffected.
    ///
    /// The one thing a timescale freeze does <i>not</i> stop is <c>Update</c> itself, so input
    /// readers (e.g. <c>PlayerController</c>) must check <see cref="IsPaused"/> before acting.
    /// </summary>
    public class PauseService : IPauseService
    {
        private readonly HashSet<object> _sources = new();

        private float _scaleBeforePause = 1f;

        public bool IsPaused => _sources.Count > 0;

        public event Action<bool> OnPauseChanged;

#region Default callbacks

        public void Start() { }

        public void OnDestroy()
        {
            OnPauseChanged = null;
            ClearAll();
        }

#endregion

        public void Pause(object source)
        {
            if (source == null)
            {
                Debug.LogError("[PauseService] Pause() requires a non-null source so it can be released later.");
                return;
            }

            bool wasPaused = IsPaused;
            if (!_sources.Add(source) || wasPaused)
                return;

            _scaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            OnPauseChanged?.Invoke(true);
        }

        public void Resume(object source)
        {
            if (source == null || !_sources.Remove(source) || IsPaused)
                return;

            Time.timeScale = _scaleBeforePause;
            OnPauseChanged?.Invoke(false);
        }

        public void ClearAll()
        {
            if (_sources.Count == 0)
                return;

            _sources.Clear();
            Time.timeScale = _scaleBeforePause;
            OnPauseChanged?.Invoke(false);
        }
    }
}
