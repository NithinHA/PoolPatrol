using System;
using UnityEngine;

namespace Player
{
    public class PlayerCombo : MonoBehaviour
    {
        public int CurrentCombo;
        [SerializeField] private PlayerController m_PlayerController;

        [Header("Combo thresholds")]
        [Tooltip("Hits needed to leave Low / Mid / High. Reduced by the \"Faster Combo\" ability.")]
        [SerializeField] private int[] m_Thresholds = { 3, 6, 9 };

        public enum ComboLevel
        {
            Low, Mid, High, Max
        }

        /// <summary>
        /// Flat reduction applied to every threshold, set by the run-modifier binder.
        /// Thresholds never fall below 1.
        /// </summary>
        public int ThresholdReduction { get; private set; }

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
            int configured = (m_Thresholds != null && index < m_Thresholds.Length)
                ? m_Thresholds[index]
                : (index + 1) * 3;
            return Mathf.Max(1, configured - ThresholdReduction);
        }

        /// <summary>
        /// Applies the "Faster Combo" upgrade. Re-raises the level event so listeners
        /// (weapons, UI) pick up a level change caused purely by the new thresholds.
        /// </summary>
        public void SetThresholdReduction(int reduction)
        {
            reduction = Mathf.Max(0, reduction);
            if (reduction == ThresholdReduction)
                return;

            ThresholdReduction = reduction;
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