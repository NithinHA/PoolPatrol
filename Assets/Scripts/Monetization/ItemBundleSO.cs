using System.Collections.Generic;
using Loadout;
using UnityEngine;

namespace Monetization
{
    /// <summary>
    /// A non-consumable IAP that grants several loadout items at once. Items are referenced directly,
    /// so a bundle can mix categories and can include items that are also sold on their own.
    /// </summary>
    [CreateAssetMenu(menuName = "PoolPatrol/Shop/Item Bundle", fileName = "ItemBundle")]
    public class ItemBundleSO : ShopOfferSO
    {
        public List<LoadoutItemSO> Items = new();

        public override bool IsConsumable => false;

        /// <summary>Summary for the card, e.g. "4 Floaties" or "Character + 2 Floaties + Weapon".</summary>
        public string ContentsLabel
        {
            get
            {
                int characters = 0, floaties = 0, weapons = 0;
                foreach (LoadoutItemSO item in Items)
                {
                    switch (item)
                    {
                        case CharacterDefinitionSO: characters++; break;
                        case FloatieDefinitionSO: floaties++; break;
                        case WeaponDefinitionSO: weapons++; break;
                    }
                }

                var parts = new List<string>(3);
                AddPart(parts, characters, "Character", "Characters");
                AddPart(parts, floaties, "Floatie", "Floaties");
                AddPart(parts, weapons, "Weapon", "Weapons");

                // "Character + Floatie" reads fine bare, but a lone item needs its count: "1 Floatie".
                return parts.Count == 1 && parts[0].IndexOf(' ') < 0 ? $"1 {parts[0]}" : string.Join(" + ", parts);
            }
        }

        private static void AddPart(List<string> parts, int count, string singular, string plural)
        {
            if (count == 1) parts.Add(singular);
            else if (count > 1) parts.Add($"{count} {plural}");
        }
    }
}
