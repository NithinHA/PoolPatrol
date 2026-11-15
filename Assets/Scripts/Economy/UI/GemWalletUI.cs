using DG.Tweening;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UI.HUD;
using UnityEngine;

namespace Economy.UI
{
    /// <summary>
    /// HUD wallet showing the current run's Gems balance. Listens to the economy service and
    /// refreshes its label whenever the balance changes; the gem icon punches when gems arrive.
    /// Put a <see cref="CollectionTargetMarker"/> (id = GemWallet) on the icon so gems fly into it.
    /// </summary>
    public class GemWalletUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_CountText;
        [Tooltip("Optional. Punched whenever the balance goes up.")]
        [SerializeField] private RectTransform m_Icon;
        [SerializeField] private float m_IconPunchScale = 0.3f;
        [SerializeField] private float m_IconPunchDuration = 0.25f;

        private IEconomyService _economy;
        private Tween _punchTween;

        private void Start()
        {
            _economy = ServiceLocator.GetEconomyService();
            if (_economy == null)
            {
                Debug.LogWarning("[GemWalletUI] EconomyService not available.");
                return;
            }

            _economy.OnBalanceChanged += OnBalanceChanged;
            Refresh(_economy.GetBalance(CurrencyType.Gems));
        }

        private void OnDestroy()
        {
            if (_economy != null)
                _economy.OnBalanceChanged -= OnBalanceChanged;
            _punchTween?.Kill();
        }

        private void OnBalanceChanged(CurrencyType type, int newBalance, int delta)
        {
            if (type != CurrencyType.Gems)
                return;

            Refresh(newBalance);

            if (delta > 0 && m_Icon != null)
            {
                // Complete (not Kill) so back-to-back gems never leave the icon mid-punch.
                _punchTween?.Complete();
                // Unscaled: gems can also be granted while the Goddess shop has time frozen.
                _punchTween = m_Icon.DOPunchScale(Vector3.one * m_IconPunchScale, m_IconPunchDuration, 6, 0.6f)
                    .SetUpdate(true);
            }
        }

        private void Refresh(int value) => HudNumberText.SetGrouped(m_CountText, value);
    }
}
