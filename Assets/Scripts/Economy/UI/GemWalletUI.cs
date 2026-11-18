using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UI.HUD;
using UnityEngine;

namespace Economy.UI
{
    /// <summary>
    /// HUD wallet showing the current run's Gems balance. Listens to the economy service and
    /// refreshes its label whenever the balance changes. Its <see cref="CollectionIconTarget"/> is
    /// where gems fly to; the icon shakes as each one arrives.
    /// </summary>
    public class GemWalletUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_CountText;
        [SerializeField] private CollectionIconTarget m_IconTarget;

        private IEconomyService _economy;

        private void OnEnable() => m_IconTarget.Enable(CollectionTargetId.GemWallet);

        private void OnDisable() => m_IconTarget.Disable();

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
        }

        private void OnBalanceChanged(CurrencyType type, int newBalance, int delta)
        {
            if (type == CurrencyType.Gems)
                Refresh(newBalance);
        }

        private void Refresh(int value) => HudNumberText.SetGrouped(m_CountText, value);
    }
}
