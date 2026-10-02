using System;
using UnityEngine;

namespace Player
{
    public class PlayerCombo : MonoBehaviour
    {
        public int CurrentCombo;
        [SerializeField] private PlayerController m_PlayerController;

        [Serializable]
        private struct ComboThresholdSet
        {
            [Tooltip("Hits needed to reach Mid / High / Max at this \"Faster Combo\" level, in order.")]
            public int[] Thresholds;
        }

        [Header("Combo thresholds")]
        [Tooltip("Hits needed to leave Low / Mid / High before any \"Faster Combo\" level is owned.")]
        [SerializeField] private int[] m_Thresholds = { 3, 6, 9 };

        [Tooltip("Threshold curve for each \"Faster Combo\" level (index 0 = level 1). Each level " +
                 "fully replaces the thresholds above rather than subtracting from them uniformly, " +
                 "so the curve can be hand-tuned per level instead of flattening every gap by the " +
                 "same amount.")]
        [SerializeField] private ComboThresholdSet[] m_FasterComboLevels =
        {
            new() { Thresholds = new[] { 3, 5, 8 } },
            new() { Thresholds = new[] { 2, 4, 7 } },
            new() { Thresholds = new[] { 2, 3, 5 } },
        };

        public enum ComboLevel
        {
            Low, Mid, High, Max
        }

        /// <summary>Current "Faster Combo" level (0 = not owned), set by the run-modifier binder.</summary>
        public int FasterComboLevel { get; private set; }

        public ComboLevel CurrentComboLevel
        {
            get
            {
                if (CurrentCombo <= Threshold(0)) return ComboLevel.Low;
                if (CurrentCombo <= Threshold(1)) return ComboLevel.Mid;
                if (CurrentCombo <= Threshold(2)) return ComboLevel.High;
                return ComboLevel.Max;
            }
        }

        private int Threshold(int index)
        {
            int[] active = ActiveThresholds();
            int configured = (active != null && index < active.Length)
                ? active[index]
                : (index + 1) * 3;
            return Mathf.Max(1, configured);
        }

        /// <summary>Picks the default thresholds, or the owned "Faster Combo" level's override curve.</summary>
        private int[] ActiveThresholds()
        {
            if (FasterComboLevel <= 0 || m_FasterComboLevels == null || m_FasterComboLevels.Length == 0)
                return m_Thresholds;

            int levelIndex = Mathf.Min(FasterComboLevel, m_FasterComboLevels.Length) - 1;
            int[] overrideThresholds = m_FasterComboLevels[levelIndex].Thresholds;
            return overrideThresholds is { Length: > 0 } ? overrideThresholds : m_Thresholds;
        }

        /// <summary>
        /// Applies the "Faster Combo" upgrade level (1-based; 0 = not owned). Re-raises the level
        /// event so listeners (weapons, UI) pick up a level change caused purely by the new
        /// thresholds.
        /// </summary>
        public void SetFasterComboLevel(int level)
        {
            level = Mathf.Max(0, level);
            if (level == FasterComboLevel)
                return;

            FasterComboLevel = level;
            OnComboLevelChanged?.Invoke(CurrentComboLevel);
        }

        public Action<ComboLevel> OnComboLevelChanged;
        public Action<int> OnComboFailed;



        public void AddCombo(Vector2 vector2)
        {
            CurrentCombo++;
            OnComboLevelChanged?.Invoke(CurrentComboLevel);
        }

        public void BreakCombo()
        {
            OnComboFailed?.Invoke(CurrentCombo);
            CurrentCombo = 0;
            OnComboLevelChanged?.Invoke(ComboLevel.Low);
        }

    }
}