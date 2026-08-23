using System;
using System.Collections.Generic;
using UnityEngine;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Default <see cref="IEconomyService"/> implementation.
    ///
    /// Each currency is an <see cref="Account"/>. Exhaustible accounts live only in memory and are
    /// wiped by <see cref="BeginRun"/>; persistent accounts are backed by PlayerPrefs and survive
    /// across runs. To add a new currency: add it to <see cref="CurrencyType"/> and register it in
    /// <see cref="Start"/>.
    /// </summary>
    public class EconomyService : IEconomyService
    {
        private class Account
        {
            public bool Exhaustible;
            public string PersistentKey;   // null/empty for exhaustible currencies
            public int Balance;
        }

        private readonly Dictionary<CurrencyType, Account> _accounts = new();

        public event Action<CurrencyType, int, int> OnBalanceChanged;

#region Default callbacks

        public void Start()
        {
            // ── Register currencies ──────────────────────────────────────────────
            // Gems: exhaustible, run-scoped. Only usable for upgrades within the current run.
            Register(CurrencyType.Gems, exhaustible: true);

            // Future non-exhaustible example (carried across runs, saved to disk):
            // Register(CurrencyType.Coins, exhaustible: false, persistentKey: "Economy_Coins");
        }

        public void OnDestroy() { }

#endregion

        private void Register(CurrencyType type, bool exhaustible, string persistentKey = null)
        {
            var account = new Account { Exhaustible = exhaustible, PersistentKey = persistentKey };

            // Persistent currencies restore their saved balance on registration.
            if (!exhaustible && !string.IsNullOrEmpty(persistentKey))
                account.Balance = PlayerPrefs.GetInt(persistentKey, 0);

            _accounts[type] = account;
        }

        public int GetBalance(CurrencyType type)
            => _accounts.TryGetValue(type, out Account account) ? account.Balance : 0;

        public void Grant(CurrencyType type, int amount)
        {
            if (amount <= 0)
                return;

            if (!_accounts.TryGetValue(type, out Account account))
            {
                Debug.LogError($"[EconomyService] Grant for unregistered currency: {type}");
                return;
            }

            account.Balance += amount;
            Persist(account);
            OnBalanceChanged?.Invoke(type, account.Balance, amount);
        }

        public bool CanAfford(CurrencyType type, int amount)
            => amount >= 0 && _accounts.TryGetValue(type, out Account account) && account.Balance >= amount;

        public bool TrySpend(CurrencyType type, int amount)
        {
            if (amount <= 0 || !CanAfford(type, amount))
                return false;

            Account account = _accounts[type];
            account.Balance -= amount;
            Persist(account);
            OnBalanceChanged?.Invoke(type, account.Balance, -amount);
            return true;
        }

        public void BeginRun()
        {
            foreach (KeyValuePair<CurrencyType, Account> pair in _accounts)
            {
                Account account = pair.Value;
                if (!account.Exhaustible || account.Balance == 0)
                    continue;

                int delta = -account.Balance;
                account.Balance = 0;
                OnBalanceChanged?.Invoke(pair.Key, 0, delta);
            }
        }

        private static void Persist(Account account)
        {
            if (!account.Exhaustible && !string.IsNullOrEmpty(account.PersistentKey))
                PlayerPrefs.SetInt(account.PersistentKey, account.Balance);
        }
    }
}
