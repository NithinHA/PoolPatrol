using System.Collections.Generic;
using System.IO;
using Abilities;
using UnityEditor;
using UnityEngine;

namespace PoolPatrol.Editor
{
    public static class CreateStarterAbilities
    {
        private const string AbilitiesFolder = "Assets/ScriptableObjects/Abilities";
        private const string DatabasePath    = "Assets/ScriptableObjects/AbilityDatabase.asset";

        [MenuItem("PoolPatrol/Create Starter Abilities")]
        public static void Create()
        {
            if (!Directory.Exists(AbilitiesFolder))
                Directory.CreateDirectory(AbilitiesFolder);

            var all = new List<AbilityDefinition>();

            // ── General ────────────────────────────────────────────────────

            // Extra Life — restores 1 lost life per purchase.
            // 5 levels so it can appear multiple times per run.
            var extraLife = CreateAbility("extra_life", "Extra Life", new()
            {
                Category        = AbilityCategory.General,
                Rarity          = AbilityRarity.Common,
                RunRequirement  = RunRequirement.LivesBelowMax,
                SelectionWeight = 1.2f,
                Levels = MakeLevels(5, i => new AbilityLevel
                {
                    Cost            = 50,
                    Description     = "Restore one life",
                    Modifiers       = new(),
                    InstantEffects  = new() { InstantEffectType.RestoreOneLife },
                }),
            });
            all.Add(extraLife);

            // Increment Max Life — raises the cap by 1 each level.
            var maxLifeUp = CreateAbility("max_life_up", "+1 Max Life", new()
            {
                Category        = AbilityCategory.General,
                Rarity          = AbilityRarity.Uncommon,
                RunRequirement  = RunRequirement.MaxLivesBelowCap,
                SelectionWeight = 0.9f,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 120,
                    Description = "Maximum lives +1",
                    Modifiers   = new() { Mod(StatId.MaxLives, ModifierType.Flat, 1f) },
                }),
            });
            all.Add(maxLifeUp);

            // Movement Speed — +15% per level.
            all.Add(CreateAbility("move_speed", "Swift Swimmer", new()
            {
                Category = AbilityCategory.General,
                Rarity   = AbilityRarity.Common,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 60 + i * 10,
                    Description = $"Move speed +{15 * (i + 1)}%",
                    Modifiers   = new() { Mod(StatId.MoveSpeed, ModifierType.Percent, 0.15f) },
                }),
            }));

            // Faster Combo — reduce combo thresholds by 1 per level.
            all.Add(CreateAbility("faster_combo", "Faster Combo", new()
            {
                Category = AbilityCategory.General,
                Rarity   = AbilityRarity.Common,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 60 + i * 10,
                    Description = $"Combo thresholds -{i + 1}",
                    Modifiers   = new() { Mod(StatId.ComboThresholdReduction, ModifierType.Flat, 1f) },
                }),
            }));

            // Gem Magnet — +1 unit collection radius per level.
            all.Add(CreateAbility("gem_magnet", "Gem Magnet", new()
            {
                Category = AbilityCategory.General,
                Rarity   = AbilityRarity.Common,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 50 + i * 10,
                    Description = $"Gem pickup range +{i + 1}",
                    Modifiers   = new() { Mod(StatId.GemMagnetRadius, ModifierType.Flat, 1f) },
                }),
            }));

            // Damage Revenge — flag ability, one-shot.
            all.Add(CreateAbility("damage_revenge", "Damage Revenge", new()
            {
                Category = AbilityCategory.General,
                Rarity   = AbilityRarity.Rare,
                Levels = new()
                {
                    new AbilityLevel
                    {
                        Cost        = 100,
                        Description = "Enemies pushed away when you lose a life",
                        Modifiers   = new() { Mod(StatId.DamageRevenge, ModifierType.Flag, 1f) },
                    }
                },
            }));

            // ── Weapon — Any ───────────────────────────────────────────────

            // Quick Reload — +20% reload speed per level.
            all.Add(CreateAbility("quick_reload", "Quick Reload", new()
            {
                Category     = AbilityCategory.Weapon,
                WeaponFilter = WeaponKind.Any,
                Rarity       = AbilityRarity.Common,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 80 + i * 10,
                    Description = $"Reload {20 * (i + 1)}% faster",
                    Modifiers   = new() { Mod(StatId.ReloadSpeed, ModifierType.Percent, 0.20f) },
                }),
            }));

            // Faster Bullets — +20% bullet speed per level.
            all.Add(CreateAbility("faster_bullets", "Faster Bullets", new()
            {
                Category     = AbilityCategory.Weapon,
                WeaponFilter = WeaponKind.Handgun | WeaponKind.Shotgun,
                Rarity       = AbilityRarity.Common,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 80 + i * 10,
                    Description = $"Bullet speed +{20 * (i + 1)}%",
                    Modifiers   = new() { Mod(StatId.BulletSpeed, ModifierType.Percent, 0.20f) },
                }),
            }));

            // Magazine Size — Flat +1 per level.
            all.Add(CreateAbility("magazine_up", "Extended Mag", new()
            {
                Category     = AbilityCategory.Weapon,
                WeaponFilter = WeaponKind.Any,
                Rarity       = AbilityRarity.Uncommon,
                Levels = MakeLevels(4, i => new AbilityLevel
                {
                    Cost        = 100,
                    Description = $"Magazine +{i + 1}",
                    Modifiers   = new() { Mod(StatId.MagazineSize, ModifierType.Flat, 1f) },
                }),
            }));

            // ── Weapon — Handgun ───────────────────────────────────────────

            // Bigger Bullets — +25% bullet size per level.
            all.Add(CreateAbility("bigger_bullets", "Bigger Bullets", new()
            {
                Category     = AbilityCategory.Weapon,
                WeaponFilter = WeaponKind.Handgun,
                Rarity       = AbilityRarity.Common,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 90,
                    Description = $"Bullet size +{25 * (i + 1)}%",
                    Modifiers   = new() { Mod(StatId.BulletSize, ModifierType.Percent, 0.25f) },
                }),
            }));

            // Bullet Range — +20% per level, Handgun.
            all.Add(CreateAbility("bullet_range", "Long Shot", new()
            {
                Category     = AbilityCategory.Weapon,
                WeaponFilter = WeaponKind.Handgun,
                Rarity       = AbilityRarity.Common,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 70 + i * 10,
                    Description = $"Bullet range +{20 * (i + 1)}%",
                    Modifiers   = new() { Mod(StatId.BulletRange, ModifierType.Percent, 0.20f) },
                }),
            }));

            // Bullet Rebound — flag, Handgun only.
            all.Add(CreateAbility("bullet_rebound", "Ricochet", new()
            {
                Category     = AbilityCategory.Weapon,
                WeaponFilter = WeaponKind.Handgun,
                Rarity       = AbilityRarity.Rare,
                SelectionWeight = 0.7f,
                Levels = new()
                {
                    new AbilityLevel
                    {
                        Cost        = 120,
                        Description = "Bullets bounce off walls",
                        Modifiers   = new() { Mod(StatId.BulletRebound, ModifierType.Flag, 1f) },
                    }
                },
            }));

            // ── Weapon — Shotgun ───────────────────────────────────────────

            // Extra Propulsion — Shotgun recoil boost.
            all.Add(CreateAbility("propulsion_boost", "Turbo Recoil", new()
            {
                Category     = AbilityCategory.Weapon,
                WeaponFilter = WeaponKind.Shotgun,
                Rarity       = AbilityRarity.Common,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 70 + i * 10,
                    Description = $"Recoil propulsion +{20 * (i + 1)}%",
                    Modifiers   = new() { Mod(StatId.Propulsion, ModifierType.Percent, 0.20f) },
                }),
            }));

            // Fire Rate — Shotgun.
            all.Add(CreateAbility("fire_rate", "Rapid Fire", new()
            {
                Category     = AbilityCategory.Weapon,
                WeaponFilter = WeaponKind.Shotgun,
                Rarity       = AbilityRarity.Uncommon,
                Levels = MakeLevels(3, i => new AbilityLevel
                {
                    Cost        = 90 + i * 10,
                    Description = $"Fire rate +{15 * (i + 1)}%",
                    Modifiers   = new() { Mod(StatId.FireRate, ModifierType.Percent, 0.15f) },
                }),
            }));

            // ── Arena-specific ─────────────────────────────────────────────

            // Piercing Shot — universal arena ability (doc says don't split).
            all.Add(CreateAbility("piercing_shot", "Piercing Shot", new()
            {
                Category = AbilityCategory.Arena,
                Rarity   = AbilityRarity.Rare,
                Levels = new()
                {
                    new AbilityLevel
                    {
                        Cost        = 100,
                        Description = "Bullets pierce shields and barriers",
                        Modifiers   = new() { Mod(StatId.PiercingShot, ModifierType.Flag, 1f) },
                    }
                },
            }));

            // Toxic Immunity.
            all.Add(CreateAbility("toxic_immunity", "Toxic Immunity", new()
            {
                Category    = AbilityCategory.Arena,
                ArenaFilter = ArenaFlags.BiohazardBasin,
                Rarity      = AbilityRarity.Uncommon,
                Levels = new()
                {
                    new AbilityLevel
                    {
                        Cost        = 100,
                        Description = "Immune to toxic contamination",
                        Modifiers   = new() { Mod(StatId.ToxicImmunity, ModifierType.Flag, 1f) },
                    }
                },
            }));

            // Storm Immunity.
            all.Add(CreateAbility("storm_immunity", "Storm Immunity", new()
            {
                Category    = AbilityCategory.Arena,
                ArenaFilter = ArenaFlags.StratosphereSpa,
                Rarity      = AbilityRarity.Uncommon,
                Levels = new()
                {
                    new AbilityLevel
                    {
                        Cost        = 100,
                        Description = "Immune to lightning zones",
                        Modifiers   = new() { Mod(StatId.StormImmunity, ModifierType.Flag, 1f) },
                    }
                },
            }));

            // Foam Breaker.
            all.Add(CreateAbility("foam_breaker", "Foam Breaker", new()
            {
                Category    = AbilityCategory.Arena,
                ArenaFilter = ArenaFlags.StratosphereSpa,
                Rarity      = AbilityRarity.Common,
                Levels = new()
                {
                    new AbilityLevel
                    {
                        Cost        = 80,
                        Description = "Bullets destroy foam faster",
                        Modifiers   = new() { Mod(StatId.FoamBreaker, ModifierType.Flag, 1f) },
                    }
                },
            }));

            // Vine Cutter.
            all.Add(CreateAbility("vine_cutter", "Vine Cutter", new()
            {
                Category    = AbilityCategory.Arena,
                ArenaFilter = ArenaFlags.FloraFalls,
                Rarity      = AbilityRarity.Common,
                Levels = new()
                {
                    new AbilityLevel
                    {
                        Cost        = 80,
                        Description = "Increased damage against vines",
                        Modifiers   = new() { Mod(StatId.VineCutter, ModifierType.Flag, 1f) },
                    }
                },
            }));

            // ── Populate database ──────────────────────────────────────────

            var database = AssetDatabase.LoadAssetAtPath<AbilityDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<AbilityDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.SetAbilities(all);
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PoolPatrol] Created {all.Count} starter abilities and populated {DatabasePath}.");
        }

        // ────────────────────────────────────────────────────────────────────

        private struct AbilitySpec
        {
            public AbilityCategory Category;
            public AbilityRarity Rarity;
            public WeaponKind WeaponFilter;
            public ArenaFlags ArenaFilter;
            public RunRequirement RunRequirement;
            public float SelectionWeight;
            public List<AbilityLevel> Levels;
        }

        private static AbilityDefinition CreateAbility(string id, string displayName, AbilitySpec spec)
        {
            string path = $"{AbilitiesFolder}/Ability_{id}.asset";

            var ability = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
            if (ability == null)
            {
                ability = ScriptableObject.CreateInstance<AbilityDefinition>();
                AssetDatabase.CreateAsset(ability, path);
            }

            ability.Id              = id;
            ability.DisplayName     = displayName;
            ability.Icon            = null;
            ability.Rarity          = spec.Rarity;
            ability.Category        = spec.Category;
            ability.WeaponFilter    = spec.WeaponFilter == 0 ? WeaponKind.Any : spec.WeaponFilter;
            ability.ArenaFilter     = spec.ArenaFilter == 0 ? ArenaFlags.Any : spec.ArenaFilter;
            ability.RunRequirement  = spec.RunRequirement;
            ability.Levels          = spec.Levels ?? new();
            ability.Prerequisites   = new();
            ability.Conflicts       = new();
            ability.SelectionWeight = spec.SelectionWeight > 0 ? spec.SelectionWeight : 1f;

            EditorUtility.SetDirty(ability);
            return ability;
        }

        private static List<AbilityLevel> MakeLevels(int count, System.Func<int, AbilityLevel> factory)
        {
            var list = new List<AbilityLevel>(count);
            for (int i = 0; i < count; i++)
                list.Add(factory(i));
            return list;
        }

        private static StatModifier Mod(StatId stat, ModifierType type, float value)
        {
            return new StatModifier { Stat = stat, Type = type, Value = value };
        }
    }
}
