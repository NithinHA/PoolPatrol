using Economy;
using Player;
using PTL.Framework;
using PTL.Framework.Services;
using SpawningLogic;
using UI.PostGame;
using UI.Shop;
using UnityEngine;

/// <summary>
/// Signal GameManager when the session ends (game-over or level-complete)
/// Own level-specific concerns: timers, win conditions, wave completion hooks
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Assign the PlayerController prefab instance here, or leave empty to auto-find.")]
    [SerializeField] private PlayerController m_PlayerController;

    [Header("Arena")]
    [Tooltip("If assigned, the level ends when this director's arena is cleared instead of on a fixed timer.")]
    [SerializeField] private ArenaDirector m_ArenaDirector;

    [Header("Level Settings")]
    [Tooltip("Fallback timer, only used when no ArenaDirector is assigned above. 0 = endless until player dies.")]
    [SerializeField] private float m_LevelDuration = 180f;

    [Header("Post-Game")]
    [Tooltip("Shown on win or loss, offering Retry / Main Menu.")]
    [SerializeField] private PostGameUI m_PostGameUI;

    [Header("Economy")]
    [Tooltip("Score → coin conversion and run-end bonuses.")]
    [SerializeField] private EconomyConfigSO m_EconomyConfig;

    [Header("Pool Goddess")]
    [Tooltip("Shop panel. Force-closed if the run ends while it is open, so the game never stays frozen.")]
    [SerializeField] private AbilityShopUI m_ShopUI;

    private int             _arenaIndex;
    private int             _levelIndex;
    private float           _elapsed;
    private bool            _levelEnded;
    private IPauseService   _pause;
    private IRunStatsService _runStats;

    private void Awake()
    {
        if (m_PlayerController == null)
            m_PlayerController = FindFirstObjectByType<PlayerController>();

        if (m_PlayerController == null)
        {
            Debug.LogError("[LevelManager] No PlayerController found in scene.");
            return;
        }

        SetLocalPlayer();

        // Read the arena/level the player picked in the Menu scene and hand its config to the
        // ArenaDirector before its own Start() runs (Awake always precedes Start scene-wide).
        _arenaIndex = GameSession.SelectedArenaIndex;
        _levelIndex = GameSession.SelectedLevelIndex;

        if (m_ShopUI == null)
            m_ShopUI = FindAnyObjectByType<AbilityShopUI>(FindObjectsInactive.Include);

        // Tell the ability service which arena this is, so arena-specific upgrades (Toxic Immunity,
        // Vine Cutter, …) are only offered where they mean something (doc §11). The weapon half of
        // the context is kept in sync separately by WeaponStatBinder.
        var arenaDefinition = ServiceLocator.GetProgressionService()?.GetArenaDefinition(_arenaIndex);
        ServiceLocator.GetAbilityService()?.SetArenaContext(
            arenaDefinition != null ? arenaDefinition.Arena : Abilities.ArenaFlags.Any);

        if (m_ArenaDirector != null)
        {
            var config = ServiceLocator.GetProgressionService()?.GetLevelConfig(_arenaIndex, _levelIndex);
            if (config != null)
                m_ArenaDirector.Configure(config);
        }
    }

    private void Start()
    {
        // Reset everything run-scoped so nothing carries over from a previous run:
        // gems, acquired abilities, the stat modifiers they applied, and any stale pause
        // (e.g. the player quit to the menu with the shop open).
        ServiceLocator.GetPauseService()?.ClearAll();
        ServiceLocator.GetEconomyService()?.BeginRun();
        ServiceLocator.GetAbilityService()?.ResetRun();
        ServiceLocator.GetRunModifierService()?.ResetRun();
        ServiceLocator.GetRunStatsService()?.BeginRun();

        _pause     = ServiceLocator.GetPauseService();
        _runStats  = ServiceLocator.GetRunStatsService();

        // The director counted its wave sections back in Awake, before BeginRun() above wiped the
        // stats; seed them here so the pause menu reads "Wave 0 / 8" rather than "0 / 0" pre-wave-1.
        if (m_ArenaDirector != null)
            _runStats?.SetWaveProgress(m_ArenaDirector.CurrentWave, m_ArenaDirector.TotalWaves);

        // Transition the game into the active-play state
        ServiceLocator.GetGameManager()?.SwitchState(GameState.InGame);

        // Subscribe to player death
        if (m_PlayerController != null)
            m_PlayerController.PlayerHealth.OnHealthChanged += OnPlayerHealthChanged;

        if (m_ArenaDirector != null)
            m_ArenaDirector.OnArenaComplete += OnLevelComplete;
    }

    private void Update()
    {
        if (_levelEnded) return;

        // Run clock. Unscaled so a timescale-slowed moment still reads as real elapsed time, but
        // held while anything (shop, pause menu) is pausing the simulation — a run's "time elapsed"
        // should mean time actually played.
        if (_pause == null || !_pause.IsPaused)
            _runStats?.Tick(Time.unscaledDeltaTime);

        // The ArenaDirector (when assigned) owns win-condition timing via OnArenaComplete; the
        // fixed timer below only applies when no director is wired up.
        if (m_ArenaDirector == null && m_LevelDuration > 0f)
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= m_LevelDuration)
                OnLevelComplete();
        }
    }

    private void OnDestroy()
    {
        if (m_PlayerController != null)
            m_PlayerController.PlayerHealth.OnHealthChanged -= OnPlayerHealthChanged;

        if (m_ArenaDirector != null)
            m_ArenaDirector.OnArenaComplete -= OnLevelComplete;
    }

    private void SetLocalPlayer()
    {
        m_PlayerController.SetLocal();
    }

    private void OnPlayerHealthChanged(int newHealth, int maxHealth)
    {
        if (newHealth <= 0)
            OnGameOver();
    }

    /// <summary>
    /// Call this when the player completes the level's objective.
    /// </summary>
    public void OnLevelComplete()
    {
        if (_levelEnded) return;
        _levelEnded = true;

        Debug.Log("[LevelManager] Level complete!");
        EndRun();
        RunSummary summary = SettleRun(won: true);
        m_PostGameUI?.Show(won: true, summary);
    }

    private void OnGameOver()
    {
        if (_levelEnded) return;
        _levelEnded = true;

        Debug.Log("[LevelManager] Game Over.");
        EndRun();
        RunSummary summary = SettleRun(won: false);
        m_PostGameUI?.Show(won: false, summary);
    }

    /// <summary>
    /// Run-end bookkeeping: on a win, records the level's progress (completion, best score/streak),
    /// then grants the coin payout — score conversion (full on a win, reduced on a loss) plus the
    /// win-only clear / first-clear / arena-completion bonuses. Coin pouches were already credited
    /// when picked up, so they only appear in the summary.
    /// </summary>
    private RunSummary SettleRun(bool won)
    {
        IRunStatsService stats = ServiceLocator.GetRunStatsService();
        int score      = stats?.Score ?? 0;
        int streak     = stats?.HighestStreak ?? 0;
        int pouchCoins = stats?.CoinsCollected ?? 0;

        LevelMilestones milestones = default;
        bool newBestScore = false;

        IProgression progression = ServiceLocator.GetProgressionService();
        if (won && progression != null)
        {
            LevelWinResult result = progression.RecordLevelWin(_arenaIndex, _levelIndex, score, streak);
            milestones   = new LevelMilestones(result.FirstClear, result.ArenaCompleted);
            newBestScore = result.NewBestScore;
        }

        RunPayout payout = default;
        if (m_EconomyConfig != null)
            payout = m_EconomyConfig.ComputePayout(won, score, milestones);
        else
            Debug.LogError("[LevelManager] No EconomyConfig assigned — no coins paid out for this run.");

        ServiceLocator.GetEconomyService()?.Grant(CurrencyType.Coins, payout.Total);

        return new RunSummary(won, score, streak, payout, pouchCoins, newBestScore);
    }

    /// <summary>
    /// Shared teardown for win and loss. Run-scoped abilities and gems persist until the next run
    /// starts (so the post-game screen can still show them), but the shop must not be left open and
    /// the game must not be left frozen.
    /// </summary>
    private void EndRun()
    {
        if (m_ShopUI != null)
            m_ShopUI.ForceClose();

        ServiceLocator.GetRunStatsService()?.EndRun();
        ServiceLocator.GetPauseService()?.ClearAll();
    }
}
