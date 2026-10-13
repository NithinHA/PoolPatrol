using System.Collections.Generic;
using Abilities;
using DG.Tweening;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UI.Transition;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGame
{
    /// <summary>
    /// The in-run pause menu (the PauseGamePopup under the In-Game Canvas).
    ///
    /// Responsibilities, in order:
    /// <list type="number">
    ///   <item>Freeze the simulation through <see cref="IPauseService"/> for as long as it is open.</item>
    ///   <item>Show the current run at a glance — wave, time played, enemies killed — read from
    ///         <see cref="IRunStatsService"/>, and every ability bought this run as an icon grid.</item>
    ///   <item>Mirror the music / SFX / haptics toggles onto <see cref="ISettingsService"/>.</item>
    ///   <item>Resume, restart the run, or quit to the main menu. The latter two hand off to
    ///         <see cref="SceneTransitionController"/> for the water wipe.</item>
    /// </list>
    ///
    /// <c>PauseGamePopup</c> (this component's GameObject) sits <b>disabled</b> in the scene by
    /// default, so a paused-but-never-opened run costs nothing. <see cref="Open"/> activates it
    /// before touching anything else; <see cref="Close"/> plays the closing animation and then
    /// deactivates it again.
    ///
    /// Because the GameObject starts disabled, this script's own <c>Awake</c> does not run until the
    /// very first <see cref="Open"/> call activates it — so the HUD's pause button cannot be wired by
    /// code here (that code would never get the chance to run). Instead, the scene wires the HUD's
    /// pause button directly to <see cref="Open"/> via a persistent <c>onClick</c> listener in the
    /// Inspector, which Unity is happy to invoke on a disabled target. <see cref="Open"/> activates
    /// the GameObject as its very first act, which makes Unity run this script's <c>Awake</c>
    /// synchronously before returning — so every field below is valid by the time the rest of
    /// <see cref="Open"/> runs.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Open / close")]
        [Tooltip("Documentation only — wired via a persistent onClick listener in the Inspector, " +
                 "not by code, since this GameObject starts disabled (see class remarks).")]
        [SerializeField] private Button m_PauseButton;

        [Tooltip("Full-screen dimmer behind the panel. Clicking it resumes, like Resume does.")]
        [SerializeField] private Button m_BackdropButton;

        [Tooltip("The panel that scales in and out. Usually the container child.")]
        [SerializeField] private RectTransform m_Panel;

        [Header("Buttons")]
        [SerializeField] private Button m_ResumeButton;
        [SerializeField] private Button m_RestartButton;
        [SerializeField] private Button m_MainMenuButton;

        [Header("Settings toggles")]
        [SerializeField] private Toggle m_MusicToggle;
        [SerializeField] private Toggle m_SfxToggle;
        [SerializeField] private Toggle m_HapticsToggle;

        [Header("Current run")]
        [Tooltip("Current / total waves, e.g. 3 / 8.")]
        [SerializeField] private TMP_Text m_WaveValueText;

        [Tooltip("Time played this run, MM:SS. Refreshed every frame while the menu is open.")]
        [SerializeField] private TMP_Text m_TimeValueText;

        [SerializeField] private TMP_Text m_EnemiesKilledValueText;

        [Header("Active abilities")]
        [Tooltip("Grid parent the ability cells are spawned under (AbilitiesBox).")]
        [SerializeField] private RectTransform m_AbilitiesBox;

        [SerializeField] private PauseAbilityCell m_AbilityCellPrefab;

        [Tooltip("Optional. Shown instead of the grid when nothing has been bought yet.")]
        [SerializeField] private GameObject m_NoAbilitiesMessage;

        [Header("Animation")]
        [Tooltip("Scale the panel starts from when opening (the zoom-in part).")]
        [SerializeField] private float m_OpenFromScale = 0.35f;

        [SerializeField] private float m_OpenDuration = 0.34f;
        [SerializeField] private float m_CloseDuration = 0.18f;

        [Tooltip("How far the panel over/under-shoots on each axis during the squash and stretch.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float m_SquashAmount = 0.12f;

        [Tooltip("Scene-transition owner. Falls back to the persistent instance when left empty.")]
        [SerializeField] private SceneTransitionController m_Transition;

        /// <summary>True from the moment Open() is called until the close animation finishes.</summary>
        public bool IsOpen { get; private set; }

        private readonly List<PauseAbilityCell> _cells = new();

        private CanvasGroup _group;
        private Sequence _sequence;

        private IPauseService    _pause;
        private IRunStatsService _runStats;
        private IAbilityService  _abilities;
        private ISettingsService _settings;

        private bool _syncingToggles;
        private bool _leaving;

#region Lifecycle

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (m_Panel == null)
                Debug.LogError("[PauseMenuUI] No panel assigned — the popup has nothing to animate.", this);

            // m_PauseButton is deliberately NOT wired here — see class remarks.
            if (m_BackdropButton != null) m_BackdropButton.onClick.AddListener(Close);
            if (m_ResumeButton != null)   m_ResumeButton.onClick.AddListener(Close);
            if (m_RestartButton != null)  m_RestartButton.onClick.AddListener(Restart);
            if (m_MainMenuButton != null) m_MainMenuButton.onClick.AddListener(GoToMainMenu);

            if (m_MusicToggle != null)   m_MusicToggle.onValueChanged.AddListener(OnMusicToggled);
            if (m_SfxToggle != null)     m_SfxToggle.onValueChanged.AddListener(OnSfxToggled);
            if (m_HapticsToggle != null) m_HapticsToggle.onValueChanged.AddListener(OnHapticsToggled);

            ResolveServices();
            SetHidden();
        }

        private void OnDestroy()
        {
            _sequence?.Kill();

            // A scene teardown with the menu still open must not leave timeScale at 0.
            _pause?.Resume(this);
        }

        private void Update()
        {
            // The only stat that changes on its own while the menu sits open.
            if (IsOpen && m_TimeValueText != null && _runStats != null)
                m_TimeValueText.text = _runStats.FormatElapsed();
        }

        /// <summary>
        /// Looks services up late. Awake can run before Bootstrap has registered anything when the
        /// Game scene is entered directly from the editor, so the lookup is retried on every open.
        /// </summary>
        private void ResolveServices()
        {
            _pause     ??= ServiceLocator.GetPauseService();
            _runStats  ??= ServiceLocator.GetRunStatsService();
            _abilities ??= ServiceLocator.GetAbilityService();
            _settings  ??= ServiceLocator.GetSettingsService();
        }

#endregion

#region Open / close

        public void Open()
        {
            if (IsOpen || _leaving)
                return;

            // First act, always: this GameObject starts disabled, so activating it is what makes
            // Unity run this script's own Awake (wiring every button/toggle below and caching
            // _group) if that hasn't happened yet. See class remarks.
            gameObject.SetActive(true);

            ResolveServices();

            // Nothing to pause once the run is over and the post-game panel owns the screen.
            if (_runStats != null && !_runStats.IsRunActive)
            {
                gameObject.SetActive(false);
                return;
            }

            IsOpen = true;
            _pause?.Pause(this);

            SyncTogglesFromSettings();
            RefreshRunStats();
            RebuildAbilities();

            _group.alpha = 0f;
            _group.interactable = true;
            _group.blocksRaycasts = true;

            PlayOpenAnimation();
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true);

            if (m_Panel != null)
            {
                // Stretch up a touch first, then snap down — the inverse of the open pop.
                _sequence.Append(m_Panel.DOScale(
                        new Vector3(1f - m_SquashAmount * 0.5f, 1f + m_SquashAmount * 0.5f, 1f),
                        m_CloseDuration * 0.3f)
                    .SetEase(Ease.OutQuad));
                _sequence.Append(m_Panel.DOScale(m_OpenFromScale, m_CloseDuration * 0.7f)
                    .SetEase(Ease.InBack));
            }

            _sequence.Join(_group.DOFade(0f, m_CloseDuration).SetEase(Ease.InQuad));
            _sequence.OnComplete(() =>
            {
                SetHidden();
                _pause?.Resume(this);
                gameObject.SetActive(false);
            });
        }

        /// <summary>
        /// Hides the popup at once, with no animation, and drops its pause. Used before a scene
        /// change so the menu never flashes back up mid-transition.
        /// </summary>
        public void ResetImmediate()
        {
            _sequence?.Kill();
            IsOpen = false;
            SetHidden();
            _pause?.Resume(this);
            gameObject.SetActive(false);
        }

        private void SetHidden()
        {
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            if (m_Panel != null)
                m_Panel.localScale = Vector3.one * m_OpenFromScale;
        }

        /// <summary>
        /// Zoom in with a squash and stretch overshoot: the panel scales up past full size wide and
        /// flat, rebounds tall and narrow, then settles. Runs on unscaled time — the game is frozen.
        /// </summary>
        private void PlayOpenAnimation()
        {
            _sequence?.Kill();
            _sequence = DOTween.Sequence().SetUpdate(true);

            if (m_Panel != null)
            {
                m_Panel.localScale = Vector3.one * m_OpenFromScale;

                float s = m_SquashAmount;
                _sequence.Append(m_Panel.DOScale(new Vector3(1f + s, 1f - s, 1f), m_OpenDuration * 0.55f)
                    .SetEase(Ease.OutQuad));
                _sequence.Append(m_Panel.DOScale(new Vector3(1f - s * 0.55f, 1f + s * 0.55f, 1f), m_OpenDuration * 0.25f)
                    .SetEase(Ease.InOutQuad));
                _sequence.Append(m_Panel.DOScale(Vector3.one, m_OpenDuration * 0.2f)
                    .SetEase(Ease.OutQuad));
            }

            _sequence.Join(_group.DOFade(1f, m_OpenDuration * 0.4f).SetEase(Ease.OutQuad));
        }

#endregion

#region Run stats

        private void RefreshRunStats()
        {
            if (_runStats == null)
                return;

            if (m_WaveValueText != null)
                m_WaveValueText.text = $"{_runStats.CurrentWave} / {_runStats.TotalWaves}";

            if (m_TimeValueText != null)
                m_TimeValueText.text = _runStats.FormatElapsed();

            if (m_EnemiesKilledValueText != null)
                m_EnemiesKilledValueText.text = _runStats.EnemiesKilled.ToString();
        }

#endregion

#region Abilities grid

        /// <summary>
        /// Rebuilds the grid from everything bought this run. Cells are reused across opens: the
        /// list only ever grows to the largest number of abilities a single run has shown.
        /// </summary>
        private void RebuildAbilities()
        {
            foreach (PauseAbilityCell cell in _cells)
                if (cell != null)
                    cell.gameObject.SetActive(false);

            int used = 0;

            if (_abilities != null && m_AbilitiesBox != null && m_AbilityCellPrefab != null)
            {
                foreach (KeyValuePair<AbilityDefinition, int> owned in _abilities.OwnedAbilities)
                {
                    if (owned.Key == null)
                        continue;

                    GetCell(used++).Bind(owned.Key, owned.Value);
                }
            }

            if (m_NoAbilitiesMessage != null)
                m_NoAbilitiesMessage.SetActive(used == 0);
        }

        private PauseAbilityCell GetCell(int index)
        {
            while (_cells.Count <= index)
                _cells.Add(Instantiate(m_AbilityCellPrefab, m_AbilitiesBox));

            return _cells[index];
        }

#endregion

#region Settings toggles

        private void SyncTogglesFromSettings()
        {
            if (_settings == null)
                return;

            _syncingToggles = true;
            if (m_MusicToggle != null)   m_MusicToggle.SetIsOnWithoutNotify(_settings.MusicEnabled);
            if (m_SfxToggle != null)     m_SfxToggle.SetIsOnWithoutNotify(_settings.SFXEnabled);
            if (m_HapticsToggle != null) m_HapticsToggle.SetIsOnWithoutNotify(_settings.HapticsEnabled);
            _syncingToggles = false;
        }

        private void OnMusicToggled(bool on)
        {
            if (_syncingToggles) return;
            _settings?.SetMusicEnabled(on);
        }

        private void OnSfxToggled(bool on)
        {
            if (_syncingToggles) return;
            _settings?.SetSFXEnabled(on);
        }

        private void OnHapticsToggled(bool on)
        {
            if (_syncingToggles) return;
            _settings?.SetHapticsEnabled(on);
        }

#endregion

#region Leaving the run

        private void Restart() => LeaveTo(Constants.SceneNames.GAME, m_RestartButton);

        private void GoToMainMenu() => LeaveTo(Constants.SceneNames.MENU, m_MainMenuButton);

        /// <summary>
        /// Resets the popup, then plays the water wipe into <paramref name="sceneName"/>. The pause
        /// is held through the wipe — otherwise the frozen gameplay visibly resumes behind the
        /// cover — and cleared once the new scene is in.
        /// </summary>
        private void LeaveTo(string sceneName, Button origin)
        {
            if (_leaving)
                return;
            _leaving = true;

            ResetImmediate();

            // Re-taken after ResetImmediate released it: this one is held for the transition, and
            // dropped by the ClearAll below once the next scene has loaded.
            _pause?.Pause(this);

            Vector2 screenOrigin = origin != null
                ? (Vector2)origin.transform.position
                : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            SceneTransitionController transition =
                m_Transition != null ? m_Transition : SceneTransitionController.Instance;

            void OnLoaded()
            {
                // The incoming scene owns the timescale from here. LevelManager clears pauses on its
                // own Start, but the Menu scene has no LevelManager — so always clear it ourselves.
                ServiceLocator.GetPauseService()?.ClearAll();
            }

            if (transition != null)
                transition.TransitionToScene(sceneName, screenOrigin, OnLoaded);
            else
                ServiceLocator.GetService<ISceneService>().LoadScene(sceneName, OnLoaded);
        }

#endregion
    }
}
