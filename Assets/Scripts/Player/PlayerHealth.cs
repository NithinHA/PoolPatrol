using System;
using UnityEngine;
using Utils;

namespace Player
{
    public class PlayerHealth : MonoBehaviour
    {
        public int MaxHealth = 5;
        public int CurrentHealth = 5;
        public float DamageCooldown = 1f;

        [Header("Effects")]
        [SerializeField] private ShakeIntensity m_DamageShake = ShakeIntensity.Medium;

        public bool IsPlayerAlive => CurrentHealth > 0;

        /// <summary>
        /// Fired whenever health changes (damage or heal). Passes (newHealth, maxHealth).
        /// </summary>
        public event Action<int, int> OnHealthChanged;

        /// <summary>
        /// Fired the instant a hit actually lands (after the damage cooldown check, so it never
        /// fires for ignored hits). Used by <c>DamageRevenge</c> to push nearby enemies away.
        /// </summary>
        public event Action OnDamaged;

        /// <summary>
        /// Seconds of damage immunity remaining. Counts down on scaled time, so it does not drain
        /// while the game is paused.
        /// </summary>
        public float ImmunityRemaining { get; private set; }

        public bool IsInvulnerable => ImmunityRemaining > 0f;

        private void Awake()
        {
            CurrentHealth = MaxHealth;
        }

        private void Update()
        {
            if (ImmunityRemaining > 0f)
                ImmunityRemaining = Mathf.Max(0f, ImmunityRemaining - Time.deltaTime);
        }

        /// <summary>
        /// Makes the player immune to damage for <paramref name="seconds"/>. Used as the
        /// re-entry grace period after the Goddess shop closes, so the player is not immediately
        /// punished by whatever moved in while the game was frozen. Never shortens an immunity
        /// that is already longer than the requested one.
        /// </summary>
        public void GrantDamageImmunity(float seconds)
        {
            if (seconds > ImmunityRemaining)
                ImmunityRemaining = seconds;
        }

        public void TakeDamage()
        {
            if (IsInvulnerable)
                return;

            if (CurrentHealth > 0)
                CurrentHealth--;

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnDamaged?.Invoke();

            CameraShaker.Instance?.Shake(m_DamageShake);

            if (!IsPlayerAlive)
                KillPlayer();
            else
                GrantDamageImmunity(DamageCooldown);
        }

        /// <summary>
        /// Restores one point of health, up to MaxHealth. Suitable for heart-drop pickups.
        /// </summary>
        public void Heal(int amount = 1)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Raises or lowers the maximum health (e.g. the "+1 Max Life" ability) and notifies
        /// listeners so the HUD can add/remove icons. An increase grants the new capacity as
        /// current health immediately too — the whole point of "+1 Max Life" is an extra life
        /// right now, not an empty slot the player has to go fill some other way. A decrease only
        /// clamps current health down; nothing in the game lowers max health today, but this keeps
        /// the method correct if something does.
        /// </summary>
        public void SetMaxHealth(int newMax)
        {
            newMax = Mathf.Max(1, newMax);
            if (newMax == MaxHealth)
                return;

            int delta = newMax - MaxHealth;
            MaxHealth = newMax;
            CurrentHealth = delta > 0
                ? Mathf.Min(CurrentHealth + delta, MaxHealth)
                : Mathf.Min(CurrentHealth, MaxHealth);

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void KillPlayer()
        {
            ImmunityRemaining = 0f;
            Debug.Log("=> Player dieded!");
        }
    }
}