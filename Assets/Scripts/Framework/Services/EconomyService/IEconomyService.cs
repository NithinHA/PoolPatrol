using System;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Manages all player currencies (see <see cref="CurrencyType"/>).
    /// Registered as a persistent service in <see cref="Bootstrap"/>.
    /// Grants/spends flow through this single service so award and cost conditions
    /// live in one place.
    /// </summary>
    public interface IEconomyService : IService
    {
        /// <summary>Fired after any balance change. Args: (currency, newBalance, delta).</summary>
        event Action<CurrencyType, int, int> OnBalanceChanged;

        /// <summary>Current balance of the given currency (0 if unknown).</summary>
        int GetBalance(CurrencyType type);

        /// <summary>Adds <paramref name="amount"/> (ignored if &lt;= 0). Applies award conditions.</summary>
        void Grant(CurrencyType type, int amount);

        /// <summary>True if the balance can cover <paramref name="amount"/>.</summary>
        bool CanAfford(CurrencyType type, int amount);

        /// <summary>Deducts <paramref name="amount"/> if affordable. Returns whether it was spent.</summary>
        bool TrySpend(CurrencyType type, int amount);

        /// <summary>
        /// Starts a fresh run: zeroes every exhaustible currency so it cannot carry
        /// into the next run. Persistent currencies are untouched. Call at level start.
        /// </summary>
        void BeginRun();
    }
}
