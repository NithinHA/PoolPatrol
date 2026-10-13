using System.Collections.Generic;
using System.IO;
using Abilities;
using Loadout;
using UnityEditor;
using UnityEngine;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// Generates placeholder loadout content (characters, floaties, weapons, pool textures) and a
    /// <see cref="LoadoutCatalogSO"/> that lists them, mirroring <see cref="CreateStarterAbilities"/>.
    /// Sprites are left empty for the artist to assign; the first entries are unlocked so the flow works
    /// out of the box. Safe to re-run — it overwrites the assets in place.
    /// </summary>
    public static class CreateLoadoutContent
    {
        private const string Root = "Assets/ScriptableObjects/Loadout";
        private const string CatalogPath = "Assets/ScriptableObjects/LoadoutCatalog.asset";

        [MenuItem("PoolPatrol/Loadout/Create Placeholder Loadout Content")]
        public static void Create()
        {
            EnsureFolder(Root);

            var catalog = LoadOrCreate<LoadoutCatalogSO>(CatalogPath);
            catalog.Characters = new List<CharacterDefinitionSO>
            {
                Character("char_classic",  "Classic Kid",  true),
                Character("char_shades",   "Cool Shades",  true),
                Character("char_pinky",    "Pinky",        true),
                Character("char_surfer",   "Surfer",       false),
                Character("char_captain",  "Captain",      false),
                Character("char_goggles",  "Goggles",      false),
            };
            catalog.Floaties = new List<FloatieDefinitionSO>
            {
                Floatie("floatie_watermelon", "Watermelon", true),
                Floatie("floatie_duck",       "Rubber Duck", true),
                Floatie("floatie_donut",      "Donut",      true),
                Floatie("floatie_unicorn",    "Unicorn",    false),
                Floatie("floatie_flamingo",   "Flamingo",   false),
            };
            catalog.Weapons = new List<WeaponDefinitionSO>
            {
                Weapon("weapon_handgun", "Handgun", WeaponKind.Handgun, true,
                    "The most basic pistol. 6 ammo per magazine. Complete magazine reload at once.",
                    "A circular shockwave damaging nearby enemies within the radius."),
                Weapon("weapon_shotgun", "Shotgun", WeaponKind.Shotgun, true,
                    "A wide-spread blaster. Fires a fan of pellets at close range for heavy burst damage.",
                    "Releases a knockback blast that shoves back every enemy it hits."),
            };
            catalog.PoolTextures = new List<PoolTextureDefinitionSO>
            {
                PoolTexture("pool_classic",   "Classic Tiles",  true),
                PoolTexture("pool_mosaic",    "Blue Mosaic",    true),
                PoolTexture("pool_lagoon",    "Lagoon",         false),
                PoolTexture("pool_neon",      "Neon Grid",      false),
            };

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CreateLoadoutContent] Wrote LoadoutCatalog with " +
                      $"{catalog.Characters.Count} characters, {catalog.Floaties.Count} floaties, " +
                      $"{catalog.Weapons.Count} weapons, {catalog.PoolTextures.Count} pool textures at {CatalogPath}.");
            Selection.activeObject = catalog;
        }

        private static CharacterDefinitionSO Character(string id, string name, bool unlocked)
        {
            var so = LoadOrCreate<CharacterDefinitionSO>($"{Root}/{id}.asset");
            so.Id = id; so.DisplayName = name; so.UnlockedByDefault = unlocked;
            EditorUtility.SetDirty(so);
            return so;
        }

        private static FloatieDefinitionSO Floatie(string id, string name, bool unlocked)
        {
            var so = LoadOrCreate<FloatieDefinitionSO>($"{Root}/{id}.asset");
            so.Id = id; so.DisplayName = name; so.UnlockedByDefault = unlocked;
            EditorUtility.SetDirty(so);
            return so;
        }

        private static WeaponDefinitionSO Weapon(string id, string name, WeaponKind kind, bool unlocked,
            string description, string comboBonus)
        {
            var so = LoadOrCreate<WeaponDefinitionSO>($"{Root}/{id}.asset");
            so.Id = id; so.DisplayName = name; so.Kind = kind; so.UnlockedByDefault = unlocked;
            so.Description = description; so.ComboBonusDescription = comboBonus;
            EditorUtility.SetDirty(so);
            return so;
        }

        private static PoolTextureDefinitionSO PoolTexture(string id, string name, bool unlocked)
        {
            var so = LoadOrCreate<PoolTextureDefinitionSO>($"{Root}/{id}.asset");
            so.Id = id; so.DisplayName = name; so.UnlockedByDefault = unlocked;
            EditorUtility.SetDirty(so);
            return so;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static void EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }
    }
}
