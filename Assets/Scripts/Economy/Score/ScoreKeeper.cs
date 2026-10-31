using System.Collections.Generic;
using Player;
using Pooling;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;
using Weapon;

namespace Economy
{
    /// <summary>
    /// Turns the player's shots and gem pickups into run score, and shows one
    /// <see cref="ScoreBubble"/> per scoring event (Docs/economy-and-progression.md → Score).
    ///
    /// Every shot is tracked from the moment it leaves the weapon. Its kills — the direct hit plus
    /// anything it causes (combo shockwave, shotgun spread, pierce) — are attributed through
    /// <see cref="WeaponAttack.OnKill"/> and collected into one group, which is scored once:
    /// <c>Σ baseValue × multiplier</c>, with <c>n</c> = kills in the group and <c>s</c> = the streak
    /// when the shot was fired. A group is scored as soon as its shot resolves, or after
    /// <see cref="EconomyConfigSO.ResolutionWindow"/> seconds with no new kill — so a piercing bullet
    /// still on its way to a wall does not hold its bubble back. A shot that kills nothing scores
    /// nothing and shows no bubble.
    ///
    /// Lives in the Game scene. Nothing in the weapon or enemy code knows it exists.
    /// </summary>
    public class ScoreKeeper : MonoBehaviour
    {
        [SerializeField] private EconomyConfigSO m_Config;

        [Tooltip("Leave empty to auto-find the player's weapon controller.")]
        [SerializeField] private PlayerWeaponController m_WeaponController;

        [Tooltip("Leave empty to auto-find. Its combo is the hit streak.")]
        [SerializeField] private PlayerController m_PlayerController;

        [SerializeField] private PoolableItemType m_BubbleType = PoolableItemType.ScoreBubble;

        /// <summary>One in-flight shot and the kills it has collected since its last scoring.</summary>
        private class ShotGroup
        {
            public WeaponAttack Attack;
            public System.Action<WeaponAttack.Kill> KillHandler;
            public int StreakAtFire;
            public int BaseValueSum;
            public int KillCount;
            public Vector3 PositionSum;
            public float LastKillTime;
            public bool Resolved;
        }

        private readonly List<ShotGroup> _groups = new();
        private IRunStatsService _runStats;
        private PlayerCombo _combo;

        private void Awake()
        {
            if (m_Config == null)
                Debug.LogError("[ScoreKeeper] No EconomyConfig assigned — nothing will be scored.", this);

            if (m_WeaponController == null)
                m_WeaponController = FindInOwnScene<PlayerWeaponController>();
            if (m_PlayerController == null)
                m_PlayerController = FindInOwnScene<PlayerController>();
        }

        /// <summary>
        /// On Retry the Game scene loads over itself, and for a frame the outgoing scene's player is
        /// still alive — a plain FindFirstObjectByType can bind to it and miss every shot of the run.
        /// </summary>
        private T FindInOwnScene<T>() where T : Component
        {
            foreach (T candidate in FindObjectsByType<T>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene == gameObject.scene)
                    return candidate;
            return null;
        }

        private void Start()
        {
            _runStats = ServiceLocator.GetRunStatsService();

            if (m_WeaponController != null)
                m_WeaponController.OnShotFired += OnShotFired;
            else
                Debug.LogError("[ScoreKeeper] No PlayerWeaponController found — kills will not score.", this);

            _combo = m_PlayerController != null ? m_PlayerController.PlayerCombo : null;
            if (_combo != null)
                _combo.OnComboLevelChanged += OnComboChanged;

            CollectableBase.OnAnyCollected += OnAnyCollected;
        }

        private void OnDestroy()
        {
            if (m_WeaponController != null)
                m_WeaponController.OnShotFired -= OnShotFired;
            if (_combo != null)
                _combo.OnComboLevelChanged -= OnComboChanged;

            CollectableBase.OnAnyCollected -= OnAnyCollected;

            foreach (ShotGroup group in _groups)
                Unbind(group);
            _groups.Clear();
        }

        private void Update()
        {
            if (_groups.Count == 0 || m_Config == null)
                return;

            // Scaled time: a pause (shop, pause menu) holds the window open rather than flushing it.
            float now = Time.time;
            for (int i = _groups.Count - 1; i >= 0; i--)
            {
                ShotGroup group = _groups[i];
                bool windowElapsed = group.KillCount > 0 && now - group.LastKillTime >= m_Config.ResolutionWindow;

                if (group.Resolved || windowElapsed)
                    ScoreGroup(group);

                if (group.Resolved)
                {
                    Unbind(group);
                    _groups.RemoveAt(i);
                }
            }
        }

#region Shots

        private void OnShotFired(WeaponAttack attack, int streakAtFire)
        {
            if (m_Config == null)
                return;

            ShotGroup group = new ShotGroup { Attack = attack, StreakAtFire = streakAtFire };

            // Instant weapons have already killed inside FireWeapon, before this subscription.
            foreach (WeaponAttack.Kill kill in attack.Kills)
                AddKill(group, kill);

            group.KillHandler = kill => AddKill(group, kill);
            attack.OnKill += group.KillHandler;
            attack.OnAttackComplete += () => group.Resolved = true;
            _groups.Add(group);
        }

        private void AddKill(ShotGroup group, WeaponAttack.Kill kill)
        {
            group.BaseValueSum += kill.ScoreValue;
            group.KillCount++;
            group.PositionSum += kill.Position;
            group.LastKillTime = Time.time;
        }

        /// <summary>Scores everything the group collected since its last scoring, then empties it.</summary>
        private void ScoreGroup(ShotGroup group)
        {
            if (group.KillCount == 0)
                return;

            int hitScore = m_Config.ComputeHitScore(group.BaseValueSum, group.StreakAtFire, group.KillCount);
            // Centroid of the killed enemies; for a single kill that is the impact point itself.
            Vector3 position = group.PositionSum / group.KillCount;

            group.BaseValueSum = 0;
            group.KillCount = 0;
            group.PositionSum = Vector3.zero;

            Award(hitScore, position);
        }

        /// <summary>Breaks the closure references; the weapon also clears the attack once it resolves.</summary>
        private static void Unbind(ShotGroup group)
        {
            if (group.Attack != null)
                group.Attack.OnKill -= group.KillHandler;
            group.Attack = null;
            group.KillHandler = null;
        }

#endregion

        private void OnAnyCollected(CollectableBase collectable)
        {
            if (m_Config == null || collectable is not GemCollectable)
                return;

            Award(m_Config.GemPickupScore, collectable.transform.position);
        }

        private void OnComboChanged(PlayerCombo.ComboLevel _)
        {
            _runStats?.ReportStreak(_combo.CurrentCombo);
        }

        private void Award(int amount, Vector3 worldPosition)
        {
            if (amount <= 0 || _runStats == null || !_runStats.IsRunActive)
                return;

            _runStats.AddScore(amount);

            if (ObjectPoolManager.Instance?.SpawnItem(m_BubbleType, worldPosition, Quaternion.identity) is ScoreBubble bubble)
                bubble.Show(amount);
        }
    }
}
