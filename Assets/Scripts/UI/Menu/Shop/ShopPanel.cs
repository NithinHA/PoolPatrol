using UnityEngine;

namespace UI.Menu
{
    /// <summary>The content behind one Shop tab. <see cref="ShopScreen"/> shows one at a time.</summary>
    public abstract class ShopPanel : MonoBehaviour
    {
        /// <summary>Re-reads the catalogs and player state. Safe to call often; cards are reused.</summary>
        public abstract void Refresh();

        /// <summary>The tab was just opened.</summary>
        public virtual void ResetView() { }
    }
}
