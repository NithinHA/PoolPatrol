using System;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;
#if UNITY_EDITOR
using DG.Tweening;
using UnityEngine.InputSystem;
#endif

namespace UI.Transition
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) owner of the loading screen and the water-splash wipe. Lives on a
    /// screen-space overlay canvas in the Bootstrap scene so it sits above every scene's camera. Other
    /// code drives scene changes through here to get the loading bar and transitions for free.
    /// </summary>
    public class SceneTransitionController : Singleton<SceneTransitionController>
    {
        [Tooltip("The transition visual used around a scene load (wave overlay, shader wipe, …).")]
        [SerializeField] private ScreenTransitionEffect m_Water;
        [SerializeField] private LoadingScreenUI m_Loading;

        protected override void Awake()
        {
            DontDestroyOnLoad = true;
            base.Awake();

            // Show the loading splash from the very first frame so the Bootstrap scene's camera-less
            // init window reads as "Loading…" instead of a black screen. Hidden once the first load ends.
            if (m_Loading != null) m_Loading.Show();
        }

        /// <summary>Water-splash cover (from <paramref name="origin"/>) → load with bar → recede.</summary>
        public void TransitionToScene(string sceneName, Vector2 origin, Action onLoaded = null)
        {
            if (m_Water != null)
                m_Water.Cover(origin, () => LoadRoutine(sceneName, revealAfter: true, onLoaded));
            else
                LoadRoutine(sceneName, revealAfter: false, onLoaded);
        }

        /// <summary>Plain loading screen (no water wipe) — used for the initial Bootstrap → Menu load.</summary>
        public void LoadWithLoadingScreen(string sceneName, Action onLoaded = null)
        {
            LoadRoutine(sceneName, revealAfter: false, onLoaded);
        }

#if UNITY_EDITOR
        private Tween _previewHoldTween;

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || m_Water == null) return;

            if (kb.uKey.wasPressedThisFrame)
            {
                _previewHoldTween?.Kill();
                m_Water.Cover(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), () =>
                {
                    _previewHoldTween = DOVirtual.DelayedCall(0.3f, () => m_Water.Reveal(), ignoreTimeScale: true);
                });
            }
        }
#endif

        private void LoadRoutine(string sceneName, bool revealAfter, Action onLoaded)
        {
            if (m_Loading != null) m_Loading.Show();

            ISceneService scenes = ServiceLocator.GetService<ISceneService>();
            scenes.LoadScene(
                sceneName,
                p => { if (m_Loading != null) m_Loading.SetProgress(p); },
                () =>
                {
                    if (m_Loading != null) m_Loading.Hide();
                    onLoaded?.Invoke();
                    if (revealAfter && m_Water != null) m_Water.Reveal();
                });
        }
    }
}
