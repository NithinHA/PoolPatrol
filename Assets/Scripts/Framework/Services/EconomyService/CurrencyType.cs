namespace PTL.Framework.Services
{
    /// <summary>
    /// All currencies known to the <see cref="EconomyService"/>.
    ///
    /// Currencies fall into two categories, configured in <see cref="EconomyService.Start"/>:
    ///   • Exhaustible  – arena/run-scoped. Reset to zero when a new run begins
    ///                    (see <see cref="IEconomyService.BeginRun"/>). e.g. Gems.
    ///   • Persistent   – carried across runs and usable in the meta-game
    ///                    (cosmetic / weapon unlocks). Saved to PlayerPrefs. e.g. Coins.
    ///
    /// Score is deliberately not a currency: it is a run stat on <see cref="IRunStatsService"/>
    /// that converts into Coins at run end.
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>Exhaustible, run-scoped currency dropped by enemies. Spent on in-run upgrades.</summary>
        Gems,

        /// <summary>
        /// Persistent meta-currency. Earned from the run-end payout and coin pouches (and IAP packs
        /// later); spent on cosmetics and weapons.
        /// </summary>
        Coins,
    }
}
