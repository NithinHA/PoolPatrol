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
            // Debug.Log($"=> Player take damage: {CurrentHealth}");
            if (!IsPlayerAlive)
            {
                KillPlayer();
            }
            else
            {
                ActivateDamageCooldown();
            }
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