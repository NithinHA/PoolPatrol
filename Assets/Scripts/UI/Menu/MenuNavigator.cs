using DG.Tweening;
using PTL.Framework;
using PTL.Framework.Services;
using UI.Transition;
using UnityEngine;

namespace UI.Menu
{
    /// <summary>
    /// Owns navigation across the three menu screens (Arena → Level → Loadout). Slides screens
    /// horizontally with DOTween, tweens the background blur to 0 on the loadout screen (so the avatar
    /// floats on the pool) and back otherwise, and kicks off the water-splash transition into the Game
    /// scene on Play. Replaces the legacy <c>MenuFlowController</c> for the new screens.
    /// </summary>
    public class MenuNavigator : MonoBehaviour
    {
        private enum ScreenId { Arena = 0, Level = 1, Loadout = 2 }

        [Header("Screens (ordered left → right)")]
        [SerializeField] private ArenaSelectionScreen m_ArenaScreen;
        [SerializeField] private LevelSelectionScreen m_LevelScreen;
        [SerializeField] private LoadoutScreen m_LoadoutScreen;

        [Header("Background")]
        [SerializeField] private MenuBackgroundBlur m_Blur;

        [Header("Transition")]
        [Tooltip("Optional. If empty, the persistent SceneTransitionController.Instance is used.")]
        [SerializeField] private SceneTransitionController m_Transition;
        [Tooltip("Origin for the water splash (usually the Play button). Falls back to screen centre.")]
        [SerializeField] private RectTransform m_PlayButtonAnchor;

        [Header("Slide")]
        [SerializeField] private float m_SlideDuration = 0.4f;
        [SerializeField] private Ease m_SlideEase = Ease.OutCubic;

        private RectTransform[] _screens;
        private int _current;
        private bool _animating;
        private int _selectedArena;
        private int _selectedLevel;

        private void Awake()
        {
            _screens = new[]
            {
                (RectTransform)m_ArenaScreen.transform,
                (RectTransform)m_LevelScreen.transform,
                (RectTransform)m_LoadoutScreen.transform,
            };

            m_ArenaScreen.Bind(OnArenaNext);
            m_LevelScreen.Bind(OnLevelNext, () => GoTo(ScreenId.Arena));
            m_LoadoutScreen.Bind(OnPlay, () => GoTo(ScreenId.Level));
        }

        private void Start()
        {
            // Everything starts on the Arena screen; the rest sit off to the right.
            float w = ScreenWidth();
            for (int i = 0; i < _screens.Length; i++)
            {
                _screens[i].anchoredPosition = new Vector2(i == 0 ? 0f : w, _screens[i].anchoredPosition.y);
                _screens[i].gameObject.SetActive(i == 0);
            }

            _current = 0;
            m_ArenaScreen.Show();
            if (m_Blur != null) m_Blur.SetBlur(m_Blur.DefaultBlur);
        }

        private void OnArenaNext(int arenaIndex)
        {
            _selectedArena = arenaIndex;
            m_LevelScreen.Show(arenaIndex);
            GoTo(ScreenId.Level);
        }

        private void OnLevelNext(int arenaIndex, int levelIndex)
        {
            _selectedArena = arenaIndex;
            _selectedLevel = levelIndex;
            m_LoadoutScreen.Show();
            GoTo(ScreenId.Loadout);
        }

        private void OnPlay()
        {
            GameSession.SelectedArenaIndex = _selectedArena;
            GameSession.SelectedLevelIndex = _selectedLevel;

            Vector2 origin = m_PlayButtonAnchor != null
                ? (Vector2)m_PlayButtonAnchor.position
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            SceneTransitionController transition = m_Transition != null ? m_Transition : SceneTransitionController.Instance;
            if (transition != null)
                transition.TransitionToScene(Constants.SceneNames.GAME, origin);
            else
                ServiceLocator.GetService<ISceneService>().LoadScene(Constants.SceneNames.GAME);
        }

        private void GoTo(ScreenId target)
        {
            int targetIndex = (int)target;
            if (_animating || targetIndex == _current) return;

            int dir = targetIndex > _current ? 1 : -1;   // forward (Next) slides screens left
            float w = ScreenWidth();

            RectTransform from = _screens[_current];
            RectTransform to = _screens[targetIndex];

            to.gameObject.SetActive(true);
            to.anchoredPosition = new Vector2(dir * w, to.anchoredPosition.y);

            _animating = true;
            to.DOAnchorPosX(0f, m_SlideDuration).SetEase(m_SlideEase).SetUpdate(true);
            from.DOAnchorPosX(-dir * w, m_SlideDuration).SetEase(m_SlideEase).SetUpdate(true)
                .OnComplete(() =>
                {
                    from.gameObject.SetActive(false);
                    _animating = false;
                });

            _current = targetIndex;
            UpdateBlur();
        }

        private void UpdateBlur()
        {
            if (m_Blur == null) return;
            if (_current == (int)ScreenId.Loadout) m_Blur.TweenToZero();
            else m_Blur.TweenToDefault();
        }

        private float ScreenWidth()
        {
            float w = _screens != null && _screens.Length > 0 ? _screens[0].rect.width : 0f;
            if (w <= 1f) w = Screen.width;   // fallback before the first layout pass
            return w;
        }
    }
}
