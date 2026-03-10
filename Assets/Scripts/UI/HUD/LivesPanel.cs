using System.Collections.Generic;
using Player;
using UnityEngine;

namespace UI.HUD
{
    /// <summary>
    /// Manages heart icons in the Lives panel.
    /// Subscribes to PlayerHealth.OnHealthChanged and animates hearts accordingly.
    /// Attach to the LivesPanel GameObject (parent of all HeartIcon children).
    /// </summary>
    public class LivesPanel : MonoBehaviour
    {
        [SerializeField] private HeartIcon m_HeartIconPrefab;

        private PlayerHealth _health;
        private readonly List<HeartIcon> _icons = new List<HeartIcon>();

        private void Start()
        {
            PlayerController localPlayer = PlayerController.Local;
            if (localPlayer == null)
            {
                Debug.LogWarning("[LivesPanel] PlayerController.Local is null. " +
                                 "Ensure PlayerController.Local is set before this Start().");
                return;
            }

            _health = localPlayer.PlayerHealth;
            _health.OnHealthChanged += OnHealthChanged;

            BuildIcons(_health.MaxHealth, _health.CurrentHealth);
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnHealthChanged -= OnHealthChanged;
        }

        // ── Initialization ──────────────────────────────────────────────────

        /// <summary>Spawns icons to match MaxHealth; shows only CurrentHealth of them.</summary>
        private void BuildIcons(int maxHealth, int currentHealth)
        {
            // Spawn any missing icons
            while (_icons.Count < maxHealth)
            {
                HeartIcon icon = Instantiate(m_HeartIconPrefab, transform);
                _icons.Add(icon);
            }

            // Set correct visibility without animation on first build
            for (int i = 0; i < _icons.Count; i++)
            {
                if (i < currentHealth)
                    _icons[i].Appear(animate: false);
                else
                    _icons[i].Disappear(animate: false);
            }
        }

        // ── Event handler ───────────────────────────────────────────────────

        private void OnHealthChanged(int newHealth, int maxHealth)
        {
            // Grow icon list if MaxHealth increased (e.g. pickup)
            while (_icons.Count < maxHealth)
            {
                HeartIcon icon = Instantiate(m_HeartIconPrefab, transform);
                _icons.Add(icon);
            }

            for (int i = 0; i < _icons.Count; i++)
            {
                bool shouldBeVisible = i < newHealth;
                bool isVisible       = _icons[i].gameObject.activeSelf;

                if (shouldBeVisible && !isVisible)
                    _icons[i].Appear(animate: true);
                else if (!shouldBeVisible && isVisible)
                    _icons[i].Disappear(animate: true);
            }
        }
    }
}
