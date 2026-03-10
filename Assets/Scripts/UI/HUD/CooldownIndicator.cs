using UnityEngine;
using UnityEngine.UI;
using Weapon;

namespace UI.HUD
{
    /// <summary>
    /// Displays the fire cooldown as a fill arc on the weapon icon.
    /// Polls Magazine.CooldownProgress each frame — but only while cooldown is active.
    /// Attach this as a child of the WeaponIcon Image.
    /// </summary>
    public class CooldownIndicator : MonoBehaviour
    {
        [Tooltip("Overlay Image set to Image Type = Filled (Radial 360).")]
        [SerializeField] private Image m_FillImage;

        [Tooltip("Color of the cooldown overlay arc.")]
        [SerializeField] private Color m_CooldownColor = new Color(1f, 1f, 1f, 0.45f);

        private WeaponMagazine _magazine;

        private void Awake()
        {
            if (m_FillImage != null)
            {
                m_FillImage.color = m_CooldownColor;
                m_FillImage.fillAmount = 0f;
                m_FillImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called by WeaponPanel when it binds to a new weapon.
        /// Pass null to detach (e.g. when no weapon is equipped).
        /// </summary>
        public void Bind(WeaponMagazine magazine)
        {
            _magazine = magazine;
        }

        private void Update()
        {
            if (_magazine == null || m_FillImage == null)
                return;

            float progress = _magazine.CooldownProgress;

            if (progress >= 1f)
            {
                // Cooldown done — hide overlay.
                if (m_FillImage.gameObject.activeSelf)
                    m_FillImage.gameObject.SetActive(false);
                return;
            }

            // Cooldown in progress — show inverse fill (full overlay shrinks as it recovers).
            if (!m_FillImage.gameObject.activeSelf)
                m_FillImage.gameObject.SetActive(true);

            // fillAmount = 1 − progress so the arc drains away as the weapon becomes ready.
            m_FillImage.fillAmount = 1f - progress;
        }
    }
}
