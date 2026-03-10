using System;
using System.Collections;
using UnityEngine;

namespace Player
{
    public class PlayerHealth : MonoBehaviour
    {
        public int MaxHealth = 5;
        public int CurrentHealth = 5;
        public float DamageCooldown = 1f;
        public bool IsPlayerAlive => CurrentHealth > 0;

        /// <summary>
        /// Fired whenever health changes (damage or heal). Passes (newHealth, maxHealth).
        /// </summary>
        public event Action<int, int> OnHealthChanged;

        private bool _isPlayerCooldownActive = false;
        private WaitForSeconds _waitForDamageCooldown;

        private void Awake()
        {
            _waitForDamageCooldown = new WaitForSeconds(DamageCooldown);
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage()
        {
            if (_isPlayerCooldownActive)
                return;

            CurrentHealth--;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (!IsPlayerAlive)
                KillPlayer();
            else
                ActivateDamageCooldown();
        }

        /// <summary>
        /// Restores one point of health, up to MaxHealth. Suitable for heart-drop pickups.
        /// </summary>
        public void Heal(int amount = 1)
        {
            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void KillPlayer()
        {
            Debug.Log("=> Player dieded!");
        }

        private void ActivateDamageCooldown()
        {
            _isPlayerCooldownActive = true;
            // start cooldown animation (player blink)
            StartCoroutine(DisableCooldownAfterDelay());
        }

        private IEnumerator DisableCooldownAfterDelay()
        {
            yield return _waitForDamageCooldown;
            _isPlayerCooldownActive = false;
            // stop cooldown animation
        }
    }
}