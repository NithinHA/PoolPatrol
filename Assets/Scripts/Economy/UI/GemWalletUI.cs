using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;

namespace Economy.UI
{
    /// <summary>
    /// HUD wallet showing the current run's Gems balance. Listens to the economy service and
    /// refreshes its label whenever the balance changes. Put a <see cref="CollectionTargetMarker"/>
    /// (id = GemWallet) on this element so gems fly here when collected.
    /// </summary>
    public class GemWalletUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_CountText;

        private IEconomyService _economy;

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

        private void Refresh(int value)
        {
            if (m_CountText != null)
                m_CountText.text = value.ToString();
        }
    }
}
