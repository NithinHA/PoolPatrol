using System;
using UnityEngine;

namespace Player
{
    public class PlayerCombo : MonoBehaviour
    {
        public int CurrentCombo;
        [SerializeField] private PlayerController m_PlayerController;

        public enum ComboLevel
        {
            Low, Mid, High, Max
        }

        public ComboLevel CurrentComboLevel
        {
            get
            {
                return CurrentCombo switch
                {
                    <= 3 => ComboLevel.Low,
                    <= 6 => ComboLevel.Mid,
                    <= 9 => ComboLevel.High,
                    _ => ComboLevel.Max
                };
            }
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