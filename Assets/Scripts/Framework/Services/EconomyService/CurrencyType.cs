namespace PTL.Framework.Services
{
    /// <summary>
    /// All currencies known to the <see cref="EconomyService"/>.
    ///
    /// Currencies fall into two categories, configured in <see cref="EconomyService.Start"/>:
    ///   • Exhaustible  – arena/run-scoped. Reset to zero when a new run begins
    ///                    (see <see cref="IEconomyService.BeginRun"/>). e.g. Gems.
    ///   • Persistent   – carried across runs and usable in the meta-game
    ///                    (character/weapon unlocks). Saved to PlayerPrefs. (future)
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>Exhaustible, run-scoped currency dropped by enemies. Spent on in-run upgrades.</summary>
        Gems,

        // Future non-exhaustible example:
        // Coins,   // persistent meta-currency for character / weapon unlocks
    }
}
