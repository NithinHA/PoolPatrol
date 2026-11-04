using System.Collections.Generic;
using System.IO;
using Abilities;
using Loadout;
using Monetization;
using PTL.Framework;
using TMPro;
using UI.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// Builds the main-menu Shop: placeholder art cropped from the mockups, dummy content, the card
    /// prefabs, and everything under [Screen4] Shop in Menu.unity.
    ///
    /// Re-runnable. Art, prefabs and scene objects are rebuilt by name; content assets are only created
    /// on the first run, so prices and items edited in the Inspector are never overwritten.
    /// </summary>
    public static class ShopWiring
    {
        private const string MenuScenePath = "Assets/Scenes/Menu.unity";
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        private const string PrefabRoot = "Assets/Prefabs/UI/Menu/ShopScreen";
        private const string ArtRoot = PrefabRoot + "/PlaceholderArt";
        private const string ContentRoot = "Assets/ScriptableObjects/Shop";
        private const string LoadoutRoot = "Assets/ScriptableObjects/LoadoutSelection";
        private const string CatalogPath = ContentRoot + "/ShopCatalog.asset";
        private const string LoadoutCatalogPath = LoadoutRoot + "/LoadoutCatalog.asset";

        private const string MockBundlePath = "Assets/Imports/Sprites/UI/Mocks/ShopScreen_Bundle.png";
        private const string MockGridPath = "Assets/Imports/Sprites/UI/Mocks/ShopScreens.png";
        private const string TubesPath = "Assets/Imports/Sprites/Tubes";
        private const string LevelCardPath = "Assets/Prefabs/UI/Menu/LevelScreen/LevelCard.prefab";
        private const string LoadoutItemPath = "Assets/Prefabs/UI/Menu/LoadoutScreen/LoadoutSelectionItem.prefab";
        private const string FontSourcePath = "Assets/Feel/MMTools/Tools/MMTween/Demo/Fonts/Lato/TTF/Lato-Bold.ttf";
        private const string FontFilePath = PrefabRoot + "/Fonts/Lato-Bold.ttf";
        private const string FontAssetPath = PrefabRoot + "/Fonts/Lato-Bold SDF.asset";

        private static readonly Color Ink = new(0.29f, 0.2f, 0.1f);
        private static readonly Color FrameBlue = new(0.1f, 0.46f, 0.8f);
        private static readonly Color BodyCream = new(0.97f, 0.91f, 0.76f);
        private static readonly Color CardCream = new(0.99f, 0.95f, 0.8f);
        private static readonly Color AwningRed = new(0.85f, 0.3f, 0.36f);
        private static readonly Color AwningCream = new(0.97f, 0.91f, 0.78f);
        private static readonly Color Ribbon = new(0.78f, 0.27f, 0.3f);
        private static readonly Color Wood = new(0.48f, 0.29f, 0.14f);
        private static readonly Color Navy = new(0.04f, 0.2f, 0.36f);
        private static readonly Color BannerBlue = new(0.1f, 0.42f, 0.72f);

        /// <summary>Sprites taken from the user's skeleton so the Shop matches the rest of the menu.</summary>
        private class Palette
        {
            public Sprite Rounded;
            public Sprite Lock;
            public Sprite Check;
            public Sprite Coin;
            public Sprite ArrowLeft;
            public Sprite ArrowRight;
            public Sprite ShopIcon;
        }

        [MenuItem("PoolPatrol/Shop/Build Shop (art, content, prefabs, scene)")]
        public static void BuildAll()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    Debug.LogError($"[ShopWiring] Save '{SceneManager.GetSceneAt(i).name}' first; nothing was changed.");
                    return;
                }
            }

            string previousScene = SceneManager.GetActiveScene().path;

            CreatePlaceholderArt();
            CreateFont();
            CreateContent();

            Scene menu = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            Transform flow = FindFlow(menu);
            Palette palette = BuildPalette(flow);

            CleanLoadoutItemPrefab();
            BuildPrefabs(palette, out ShopCoinPackCard coinPack, out ShopBundleCard bundle, out ShopItemCard item, out GameObject tab);
            WireMenuScene(menu, flow, palette, coinPack, bundle, item, tab);
            WireBootstrap();

            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(previousScene) && previousScene != MenuScenePath)
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);

            Debug.Log("[ShopWiring] Shop built.");
        }

#region Font

        /// <summary>
        /// A dynamic Lato font for all Shop text. Store prices arrive as localized strings in any
        /// currency, and the project's default Liberation Sans has no ₹ glyph while Lato covers the
        /// common currency signs. The TTF is copied out of Feel so the Shop doesn't depend on a demo folder.
        /// </summary>
        private static void CreateFont()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null) return;

            EnsureFolder($"{PrefabRoot}/Fonts");
            AssetDatabase.CopyAsset(FontSourcePath, FontFilePath);

            var source = AssetDatabase.LoadAssetAtPath<Font>(FontFilePath);
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 64, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            AssetDatabase.CreateAsset(font, FontAssetPath);

            font.atlasTextures[0].name = "Lato-Bold Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            font.material.name = "Lato-Bold Material";
            AssetDatabase.AddObjectToAsset(font.material, font);

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }

#endregion

#region Placeholder art

        private readonly struct Crop
        {
            public readonly string Name;
            public readonly RectInt Rect;       // pixels, origin top-left, at the mock's 1536-wide size
            public readonly bool KeyBackground;

            public Crop(string name, int x, int y, int w, int h, bool keyBackground = true)
            {
                Name = name;
                Rect = new RectInt(x, y, w, h);
                KeyBackground = keyBackground;
            }
        }

        private static readonly Crop[] BundleMockCrops =
        {
            new("pack_pile", 200, 318, 150, 112),
            new("pack_stack", 520, 316, 160, 114),
            new("pack_sack", 835, 306, 185, 124),
            new("pack_chest", 1165, 310, 200, 122),
            new("banner_flags", 125, 686, 300, 102, false),
            new("banner_football", 455, 686, 290, 102, false),
            new("banner_pirate", 775, 680, 290, 108, false),
            new("banner_tropical", 1095, 680, 293, 108, false),
            new("tab_bundles", 165, 158, 68, 64),
            new("tab_characters", 490, 162, 60, 60),
            new("tab_floaties", 840, 164, 58, 56),
            new("tab_weapons", 1155, 162, 80, 56),
        };

        private static readonly Crop[] GridMockCrops =
        {
            new("banner_mega", 512, 335, 205, 68, false),
            new("weapon_bubble", 1243, 683, 100, 72),
            new("weapon_harpoon", 1378, 681, 112, 76),
            new("weapon_triple", 963, 842, 100, 74),
            new("weapon_cannon", 1238, 838, 110, 78),
        };

        private static void CreatePlaceholderArt()
        {
            EnsureFolder(ArtRoot);
            WriteCrops(MockBundlePath, BundleMockCrops);
            WriteCrops(MockGridPath, GridMockCrops);
            WriteCoin();
            AssetDatabase.Refresh();

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single)
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void WriteCrops(string mockPath, Crop[] crops)
        {
            Texture2D mock = LoadReadable(mockPath);
            float scale = mock.width / 1536f;

            foreach (Crop crop in crops)
            {
                int x = Mathf.RoundToInt(crop.Rect.x * scale);
                int w = Mathf.RoundToInt(crop.Rect.width * scale);
                int h = Mathf.RoundToInt(crop.Rect.height * scale);
                int y = mock.height - Mathf.RoundToInt(crop.Rect.y * scale) - h;

                Color[] pixels = mock.GetPixels(x, y, w, h);
                if (crop.KeyBackground) KeyOutBackground(pixels, w, h);
                SavePng(crop.Name, pixels, w, h);
            }

            Object.DestroyImmediate(mock);
        }

        /// <summary>The coin from the mock's wallet, cut out with a circle since it sits on a dark pill.</summary>
        private static void WriteCoin()
        {
            Texture2D mock = LoadReadable(MockBundlePath);
            float scale = mock.width / 1536f;
            int size = Mathf.RoundToInt(44 * scale);
            int x = Mathf.RoundToInt(1263 * scale);
            int y = mock.height - Mathf.RoundToInt(29 * scale) - size;

            Color[] pixels = mock.GetPixels(x, y, size, size);
            float radius = size * 0.5f;
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float distance = Vector2.Distance(new Vector2(px + 0.5f, py + 0.5f), new Vector2(radius, radius));
                    pixels[py * size + px].a = Mathf.Clamp01(radius - 0.5f - distance);
                }

            SavePng("coin", pixels, size, size);
            Object.DestroyImmediate(mock);
        }

        /// <summary>Flood-fills transparency in from the crop's edges, so colours inside the art survive.</summary>
        private static void KeyOutBackground(Color[] pixels, int width, int height)
        {
            const float tolerance = 0.11f;
            var queue = new Queue<int>();
            var seen = new bool[pixels.Length];

            void Seed(int index)
            {
                if (seen[index]) return;
                seen[index] = true;
                queue.Enqueue(index);
            }

            for (int x = 0; x < width; x++) { Seed(x); Seed((height - 1) * width + x); }
            for (int y = 0; y < height; y++) { Seed(y * width); Seed(y * width + width - 1); }

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                Color background = pixels[index];
                pixels[index].a = 0f;

                int x = index % width;
                int y = index / width;
                TryExpand(x - 1, y);
                TryExpand(x + 1, y);
                TryExpand(x, y - 1);
                TryExpand(x, y + 1);

                void TryExpand(int nx, int ny)
                {
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;
                    int next = ny * width + nx;
                    if (seen[next]) return;

                    Color c = pixels[next];
                    float distance = Mathf.Abs(c.r - background.r) + Mathf.Abs(c.g - background.g) + Mathf.Abs(c.b - background.b);
                    if (distance < tolerance) Seed(next);
                }
            }
        }

        private static void SavePng(string name, Color[] pixels, int width, int height)
        {
            var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            output.SetPixels(pixels);
            output.Apply();
            File.WriteAllBytes($"{ArtRoot}/{name}.png", output.EncodeToPNG());
            Object.DestroyImmediate(output);
        }

        private static Texture2D LoadReadable(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        private static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{name}.png");

#endregion

#region Content

        private static void CreateContent()
        {
            EnsureFolder($"{ContentRoot}/CoinPacks");
            EnsureFolder($"{ContentRoot}/Bundles");

            // Runs once. After that the content belongs to the user: edited prices, or an item they deleted
            // on purpose, must survive a rebuild of the prefabs and scene.
            if (AssetDatabase.LoadAssetAtPath<ShopCatalogSO>(CatalogPath) != null) return;

            var loadout =AssetDatabase.LoadAssetAtPath<LoadoutCatalogSO>(LoadoutCatalogPath);

            FloatieDefinitionSO lifeguard = NewFloatie("floatie_lifeguard", "Lifeguard Ring", "floatie_lifeguard", UnlockType.Coins, loadout, price: 500);
            FloatieDefinitionSO polka = NewFloatie("floatie_polka", "Polka Dots", "floatie_polka_dots", UnlockType.IAP, loadout, product: "com.poolpatrol.floatie.polka", fallback: "₹30");
            FloatieDefinitionSO america = NewFloatie("floatie_america", "USA Flag", "floatie_america", UnlockType.BundleOnly, loadout);
            FloatieDefinitionSO orange = NewFloatie("floatie_orange", "Orange Flag", "floatie_orange", UnlockType.BundleOnly, loadout);
            FloatieDefinitionSO wheel = NewFloatie("floatie_color_wheel", "Color Wheel", "floatie_color_wheel", UnlockType.BundleOnly, loadout);
            FloatieDefinitionSO tropical = NewFloatie("floatie_tropical", "Tropical", "floatie_tropical", UnlockType.BundleOnly, loadout);

            WeaponDefinitionSO bubble = NewWeapon("weapon_bubble", "Bubble Gun", "weapon_bubble", WeaponKind.Handgun, UnlockType.Coins, loadout, price: 500);
            WeaponDefinitionSO harpoon = NewWeapon("weapon_harpoon", "Harpoon", "weapon_harpoon", WeaponKind.Handgun, UnlockType.IAP, loadout, product: "com.poolpatrol.weapon.harpoon", fallback: "₹30");
            WeaponDefinitionSO triple = NewWeapon("weapon_triple", "Triple Shot", "weapon_triple", WeaponKind.Shotgun, UnlockType.ArenaClear, loadout);
            WeaponDefinitionSO cannon = NewWeapon("weapon_cannon", "Cannon", "weapon_cannon", WeaponKind.Shotgun, UnlockType.IAP, loadout, product: "com.poolpatrol.weapon.cannon", fallback: "₹60");

            CharacterDefinitionSO captain = FindCharacter(loadout, "char_captain");
            CharacterDefinitionSO surfer = FindCharacter(loadout, "char_surfer");
            CharacterDefinitionSO goggles = FindCharacter(loadout, "char_goggles");
            var flamingo = AssetDatabase.LoadAssetAtPath<FloatieDefinitionSO>($"{LoadoutRoot}/Floaties/floatie_flamingo.asset");
            var unicorn = AssetDatabase.LoadAssetAtPath<FloatieDefinitionSO>($"{LoadoutRoot}/Floaties/floatie_unicorn.asset");

            // Moves one existing item to IAP so the Characters tab has one of each unlock type.
            if (captain != null)
            {
                captain.Unlock = UnlockType.IAP;
                captain.IapProductId = "com.poolpatrol.char.captain";
                captain.IapFallbackPrice = "₹30";
                EditorUtility.SetDirty(captain);
            }

            EditorUtility.SetDirty(loadout);

            ShopCatalogSO catalog = LoadOrCreate<ShopCatalogSO>(CatalogPath, out _);
            catalog.CoinPacks = new List<CoinPackSO>
            {
                NewCoinPack("pack_pile", "Pile of Coins", 500, "₹49"),
                NewCoinPack("pack_stack", "Stack of Coins", 1200, "₹99"),
                NewCoinPack("pack_sack", "Sack of Coins", 2800, "₹199"),
                NewCoinPack("pack_chest", "Chest of Coins", 6000, "₹399"),
            };
            catalog.Bundles = new List<ItemBundleSO>
            {
                NewBundle("bundle_flags", "Country Flags Pack", "banner_flags", "₹249", america, orange),
                NewBundle("bundle_football", "Football Fans Pack", "banner_football", "₹249", wheel, polka),
                NewBundle("bundle_pirate", "Pirate Pack", "banner_pirate", "₹299", captain, lifeguard, cannon),
                NewBundle("bundle_tropical", "Tropical Pack", "banner_tropical", "₹299", surfer, tropical, triple),
                NewBundle("bundle_mega", "Mega Pool Pack", "banner_mega", "₹599", goggles, flamingo, unicorn, bubble, harpoon),
            };
            EditorUtility.SetDirty(catalog);
        }

        private static FloatieDefinitionSO NewFloatie(string id, string displayName, string sprite, UnlockType unlock,
            LoadoutCatalogSO loadout, int price = 300, string product = "", string fallback = "")
        {
            var so = LoadOrCreate<FloatieDefinitionSO>($"{LoadoutRoot}/Floaties/{id}.asset", out bool created);
            if (created)
            {
                Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>($"{TubesPath}/{sprite}.png");
                so.BodySprite = art;
                FillItem(so, id, displayName, art, unlock, price, product, fallback);
            }
            if (!loadout.Floaties.Contains(so)) loadout.Floaties.Add(so);
            return so;
        }

        private static WeaponDefinitionSO NewWeapon(string id, string displayName, string art, WeaponKind kind, UnlockType unlock,
            LoadoutCatalogSO loadout, int price = 300, string product = "", string fallback = "")
        {
            var so = LoadOrCreate<WeaponDefinitionSO>($"{LoadoutRoot}/Weapons/{id}.asset", out bool created);
            if (created)
            {
                // Placeholder gameplay: dummy weapons borrow an existing weapon's kind, text and behaviour.
                WeaponDefinitionSO source = loadout.Weapons.Find(w => w != null && w.Kind == kind);
                so.Kind = kind;
                so.Description = source != null ? source.Description : string.Empty;
                so.ComboBonusDescription = source != null ? source.ComboBonusDescription : string.Empty;
                FillItem(so, id, displayName, Art(art), unlock, price, product, fallback);
            }
            if (!loadout.Weapons.Contains(so)) loadout.Weapons.Add(so);
            return so;
        }

        private static void FillItem(LoadoutItemSO so, string id, string displayName, Sprite icon, UnlockType unlock,
            int price, string product, string fallback)
        {
            so.Id = id;
            so.DisplayName = displayName;
            so.Icon = icon;
            so.Unlock = unlock;
            so.CoinPrice = price;
            so.IapProductId = product;
            if (!string.IsNullOrEmpty(fallback)) so.IapFallbackPrice = fallback;
            EditorUtility.SetDirty(so);
        }

        private static CoinPackSO NewCoinPack(string id, string displayName, int coins, string price)
        {
            var so = LoadOrCreate<CoinPackSO>($"{ContentRoot}/CoinPacks/{id}.asset", out _);
            so.DisplayName = displayName;
            so.Icon = Art(id);
            so.Coins = coins;
            so.ProductId = $"com.poolpatrol.coins.{id.Replace("pack_", string.Empty)}";
            so.FallbackPrice = price;
            EditorUtility.SetDirty(so);
            return so;
        }

        private static ItemBundleSO NewBundle(string id, string displayName, string banner, string price, params LoadoutItemSO[] items)
        {
            var so = LoadOrCreate<ItemBundleSO>($"{ContentRoot}/Bundles/{id}.asset", out _);
            so.DisplayName = displayName;
            so.Icon = Art(banner);
            so.ProductId = $"com.poolpatrol.bundle.{id.Replace("bundle_", string.Empty)}";
            so.FallbackPrice = price;
            so.Items = new List<LoadoutItemSO>();
            foreach (LoadoutItemSO item in items)
                if (item != null) so.Items.Add(item);
            EditorUtility.SetDirty(so);
            return so;
        }

        private static CharacterDefinitionSO FindCharacter(LoadoutCatalogSO loadout, string id)
            => loadout.Characters.Find(c => c != null && c.Id == id);

        private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            created = existing == null;
            if (!created) return existing;

            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

#endregion

#region Prefabs

        private static Palette BuildPalette(Transform flow)
        {
            Transform s1 = flow.Find("[Screen1] ArenaSelection");
            Transform shop = flow.Find("[Screen4] Shop");
            var card = AssetDatabase.LoadAssetAtPath<GameObject>(LevelCardPath);
            Transform lockIcon = card != null ? card.transform.Find("Locked/icon_lock") : null;

            return new Palette
            {
                Rounded = shop.Find("Container").GetComponent<Image>().sprite,
                Lock = lockIcon != null ? lockIcon.GetComponent<Image>().sprite : null,
                Check = FindCheckSprite(),
                Coin = Art("coin"),
                ArrowLeft = s1.Find("Arenas/Btn_Left").GetComponent<Image>().sprite,
                ArrowRight = s1.Find("Arenas/Btn_Right").GetComponent<Image>().sprite,
                ShopIcon = s1.Find("Btn_Shop/shop_icon").GetComponent<Image>().sprite,
            };
        }

        /// <summary>The menu's icon sheet has a tick; fall back to the built-in checkmark if it's renamed.</summary>
        private static Sprite FindCheckSprite()
        {
            foreach (string guid in AssetDatabase.FindAssets("iconSet_check t:Sprite"))
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                    if (asset is Sprite sprite && sprite.name.Contains("check"))
                        return sprite;

            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        }

        private static void CleanLoadoutItemPrefab()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(LoadoutItemPath);
            DestroyChild(contents.transform, "locked");
            PrefabUtility.SaveAsPrefabAsset(contents, LoadoutItemPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static void BuildPrefabs(Palette p, out ShopCoinPackCard coinPack, out ShopBundleCard bundle, out ShopItemCard item, out GameObject tab)
        {
            EnsureFolder(PrefabRoot);
            Scene stage = EditorSceneManager.NewPreviewScene();

            GameObject price = BuildPriceTag(p, stage);
            item = BuildItemCard(p, price, stage);
            coinPack = BuildCoinPackCard(p, price, stage);
            bundle = BuildBundleCard(p, price, stage);
            tab = BuildTabButton(p, stage);

            EditorSceneManager.ClosePreviewScene(stage);
        }

        private static GameObject BuildPriceTag(Palette p, Scene stage)
        {
            Image root = NewRoot<Image>("ShopPriceTag", stage);
            root.sprite = p.Rounded;
            root.type = Image.Type.Sliced;
            root.raycastTarget = true;

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root;

            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 8f;
            row.padding = new RectOffset(10, 10, 4, 4);
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            GameObject coin = PillIcon("coin_icon", root.transform, p.Coin);
            GameObject check = PillIcon("check_icon", root.transform, p.Check);
            GameObject lockIcon = PillIcon("lock_icon", root.transform, p.Lock);

            TMP_Text label = NewText("label", root.transform, 30f, Color.white);
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.text = "000";

            var tag = root.gameObject.AddComponent<ShopPriceTag>();
            SetRef(tag, "m_Button", button);
            SetRef(tag, "m_Background", root);
            SetRef(tag, "m_Label", label);
            SetRef(tag, "m_CoinIcon", coin);
            SetRef(tag, "m_CheckIcon", check);
            SetRef(tag, "m_LockIcon", lockIcon);

            return Save(root.gameObject);
        }

        private static GameObject PillIcon(string name, Transform parent, Sprite sprite)
        {
            Image icon = NewImage(name, parent, sprite, Color.white);
            icon.preserveAspect = true;
            var layout = icon.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 38f;
            layout.preferredHeight = 38f;
            return icon.gameObject;
        }

        private static ShopItemCard BuildItemCard(Palette p, GameObject price, Scene stage)
        {
            Image root = NewCard("ShopItemCard", p, stage);

            TMP_Text name = NewText("name_text", root.transform, 30f, Ink);
            AnchorTop(name.rectTransform, 8f, 40f, 10f);
            name.overflowMode = TextOverflowModes.Ellipsis;

            Image icon = NewImage("icon", root.transform, null, Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.anchorMin = Vector2.zero;
            icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = new Vector2(16f, 66f);
            icon.rectTransform.offsetMax = new Vector2(-16f, -50f);

            ShopPriceTag tag = NestPrice(price, root.transform, 0.08f, 10f, 50f);

            var card = root.gameObject.AddComponent<ShopItemCard>();
            SetRef(card, "m_NameText", name);
            SetRef(card, "m_Icon", icon);
            SetRef(card, "m_Price", tag);

            return Save(root.gameObject).GetComponent<ShopItemCard>();
        }

        private static ShopCoinPackCard BuildCoinPackCard(Palette p, GameObject price, Scene stage)
        {
            Image root = NewCard("ShopCoinPackCard", p, stage);
            root.gameObject.AddComponent<LayoutElement>().preferredWidth = 420f;

            Image icon = NewImage("icon", root.transform, null, Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.anchorMin = Vector2.zero;
            icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = new Vector2(16f, 124f);
            icon.rectTransform.offsetMax = new Vector2(-16f, -6f);

            TMP_Text name = NewText("name_text", root.transform, 26f, Ink);
            AnchorBottom(name.rectTransform, 90f, 34f, 10f);

            var amount = new GameObject("coins_row", typeof(RectTransform)).AddComponent<HorizontalLayoutGroup>();
            amount.transform.SetParent(root.transform, false);
            amount.childAlignment = TextAnchor.MiddleCenter;
            amount.spacing = 8f;
            amount.childControlWidth = true;
            amount.childControlHeight = true;
            amount.childForceExpandWidth = false;
            amount.childForceExpandHeight = false;
            AnchorBottom((RectTransform)amount.transform, 54f, 38f, 10f);

            PillIcon("coin_icon", amount.transform, p.Coin);
            TMP_Text coins = NewText("coins_text", amount.transform, 28f, Ink);
            coins.overflowMode = TextOverflowModes.Overflow;
            coins.text = "0";

            ShopPriceTag tag = NestPrice(price, root.transform, 0.08f, 8f, 46f);

            var card = root.gameObject.AddComponent<ShopCoinPackCard>();
            SetRef(card, "m_NameText", name);
            SetRef(card, "m_Icon", icon);
            SetRef(card, "m_CoinsText", coins);
            SetRef(card, "m_Price", tag);

            return Save(root.gameObject).GetComponent<ShopCoinPackCard>();
        }

        private static ShopBundleCard BuildBundleCard(Palette p, GameObject price, Scene stage)
        {
            Image root = NewCard("ShopBundleCard", p, stage);
            root.gameObject.AddComponent<LayoutElement>().preferredWidth = 420f;

            // Everything above the fixed footer belongs to the banner.
            Image bannerBg = NewImage("banner_bg", root.transform, p.Rounded, BannerBlue);
            bannerBg.type = Image.Type.Sliced;
            bannerBg.rectTransform.anchorMin = Vector2.zero;
            bannerBg.rectTransform.anchorMax = Vector2.one;
            bannerBg.rectTransform.offsetMin = new Vector2(8f, 118f);
            bannerBg.rectTransform.offsetMax = new Vector2(-8f, -6f);

            TMP_Text name = NewText("name_text", bannerBg.transform, 25f, Color.white);
            AnchorTop(name.rectTransform, 1f, 32f, 6f);

            Image banner = NewImage("banner", bannerBg.transform, null, Color.white);
            banner.preserveAspect = true;
            banner.rectTransform.anchorMin = Vector2.zero;
            banner.rectTransform.anchorMax = Vector2.one;
            banner.rectTransform.offsetMin = new Vector2(4f, 3f);
            banner.rectTransform.offsetMax = new Vector2(-4f, -33f);

            var row = new GameObject("item_icons", typeof(RectTransform)).AddComponent<HorizontalLayoutGroup>();
            row.transform.SetParent(root.transform, false);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 8f;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            AnchorBottom((RectTransform)row.transform, 82f, 34f, 10f);

            Image template = NewImage("item_icon", row.transform, null, Color.white);
            template.preserveAspect = true;
            template.rectTransform.sizeDelta = new Vector2(32f, 32f);
            template.gameObject.SetActive(false);

            TMP_Text contents = NewText("contents_text", root.transform, 22f, Ink);
            AnchorBottom(contents.rectTransform, 54f, 28f, 8f);

            ShopPriceTag tag = NestPrice(price, root.transform, 0.08f, 8f, 46f);

            var card = root.gameObject.AddComponent<ShopBundleCard>();
            SetRef(card, "m_NameText", name);
            SetRef(card, "m_Banner", banner);
            SetRef(card, "m_ItemIconRow", row.transform);
            SetRef(card, "m_ItemIconTemplate", template);
            SetRef(card, "m_ContentsText", contents);
            SetRef(card, "m_Price", tag);

            return Save(root.gameObject).GetComponent<ShopBundleCard>();
        }

        private static GameObject BuildTabButton(Palette p, Scene stage)
        {
            Image root = NewRoot<Image>("ShopTabButton", stage);
            root.sprite = p.Rounded;
            root.type = Image.Type.Sliced;
            root.raycastTarget = true;

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root;

            Image icon = NewImage("icon", root.transform, null, Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.anchorMin = new Vector2(0f, 0f);
            icon.rectTransform.anchorMax = new Vector2(0f, 1f);
            icon.rectTransform.pivot = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(80f, -16f);

            TMP_Text label = NewText("label", root.transform, 36f, Ink);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(110f, 0f);
            label.rectTransform.offsetMax = new Vector2(-12f, 0f);
            label.text = "Tab";

            var tab = root.gameObject.AddComponent<ShopTabButton>();
            SetRef(tab, "m_Button", button);
            SetRef(tab, "m_Background", root);
            SetRef(tab, "m_Label", label);

            return Save(root.gameObject);
        }

        private static Image NewCard(string name, Palette p, Scene stage)
        {
            Image root = NewRoot<Image>(name, stage);
            root.sprite = p.Rounded;
            root.type = Image.Type.Sliced;
            root.color = CardCream;
            return root;
        }

        private static ShopPriceTag NestPrice(GameObject pricePrefab, Transform parent, float insetFraction, float bottom, float height)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(pricePrefab, parent);
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = new Vector2(insetFraction, 0f);
            rect.anchorMax = new Vector2(1f - insetFraction, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(0f, height);
            return instance.GetComponent<ShopPriceTag>();
        }

        private static T NewRoot<T>(string name, Scene stage) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, stage);
            return go.AddComponent<T>();
        }

        private static GameObject Save(GameObject root)
        {
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/{root.name}.prefab");
            Object.DestroyImmediate(root);
            return asset;
        }

#endregion

#region Menu scene

        private static void WireMenuScene(Scene scene, Transform flow, Palette p, ShopCoinPackCard coinPack,
            ShopBundleCard bundle, ShopItemCard item, GameObject tab)
        {
            Transform s1 = flow.Find("[Screen1] ArenaSelection");
            Transform s3 = flow.Find("[Screen3] LoadoutSelection");
            var shopRoot = (RectTransform)flow.Find("[Screen4] Shop");
            Transform container = shopRoot.Find("Container");

            // The skeleton's root Image is the mockup used as a layout reference. Keep it only as an
            // invisible blocker, so taps can't reach anything behind the Shop.
            var rootImage = shopRoot.GetComponent<Image>();
            rootImage.sprite = null;
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = true;

            StyleChrome(shopRoot, container, p);

            Transform tabs = container.Find("Tabs");
            ShopTabButton[] tabButtons = BuildTabs(tabs, tab);

            Transform body = container.Find("Body");
            ShopPanel bundles = BuildBundlesPanel(body, p, coinPack, bundle);
            ShopPanel characters = BuildItemsPanel(body, "Panel_Characters", ShopCategory.Characters, item);
            ShopPanel floaties = BuildItemsPanel(body, "Panel_Floaties", ShopCategory.Floaties, item);
            ShopPanel weapons = BuildItemsPanel(body, "Panel_Weapons", ShopCategory.Weapons, item);

            Button restore = BuildRestoreButton(container);
            BuildStatus(shopRoot, p, out CanvasGroup status, out TMP_Text statusText);

            // Draw order: awning and tabs behind the body, then the sign and close button above it all.
            container.Find("Roof").SetSiblingIndex(0);
            tabs.SetSiblingIndex(1);
            body.SetSiblingIndex(2);
            container.Find("Title").SetAsLastSibling();
            restore.transform.SetAsLastSibling();
            container.Find("Btn_Back").SetAsLastSibling();

            var screen = GetOrAdd<ShopScreen>(shopRoot.gameObject);
            var so = new SerializedObject(screen);
            SerializedProperty tabsProp = so.FindProperty("m_Tabs");
            ShopPanel[] panels = { bundles, characters, floaties, weapons };
            tabsProp.arraySize = panels.Length;
            for (int i = 0; i < panels.Length; i++)
            {
                SerializedProperty element = tabsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Button").objectReferenceValue = tabButtons[i];
                element.FindPropertyRelative("Panel").objectReferenceValue = panels[i];
            }

            Transform wallet = shopRoot.Find("Wallet_Coin");
            so.FindProperty("m_CoinBalanceText").objectReferenceValue = wallet.Find("Text (TMP)").GetComponent<TMP_Text>();
            so.FindProperty("m_Wallet").objectReferenceValue = wallet;
            so.FindProperty("m_CloseButton").objectReferenceValue = container.Find("Btn_Back").GetComponent<Button>();
            so.FindProperty("m_RestoreButton").objectReferenceValue = restore;
            so.FindProperty("m_Status").objectReferenceValue = status;
            so.FindProperty("m_StatusText").objectReferenceValue = statusText;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetRef(flow.GetComponent<MenuNavigator>(), "m_ShopScreen", screen);
            SetRef(s1.GetComponent<ArenaSelectionScreen>(), "m_ShopButton", s1.Find("Btn_Shop").GetComponent<Button>());

            // Locked items now live in the Shop, so the loadout's purchase panel has nothing to do.
            DestroyChild(s3, "purchase_panel");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void StyleChrome(RectTransform shopRoot, Transform container, Palette p)
        {
            var frame = container.GetComponent<Image>();
            frame.color = FrameBlue;
            frame.type = Image.Type.Sliced;

            // Awning: alternating stripes across the top of the frame.
            Transform roof = container.Find("Roof");
            ClearChildren(roof);
            roof.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var stripes = GetOrAdd<HorizontalLayoutGroup>(roof.gameObject);
            stripes.childControlWidth = true;
            stripes.childControlHeight = true;
            stripes.childForceExpandWidth = true;
            stripes.childForceExpandHeight = true;
            for (int i = 0; i < 18; i++)
            {
                Image stripe = NewImage($"stripe_{i}", roof, p.Rounded, i % 2 == 0 ? AwningRed : AwningCream);
                stripe.type = Image.Type.Sliced;
            }
            var roofRect = (RectTransform)roof;
            roofRect.anchoredPosition = new Vector2(0f, 14f);
            roofRect.sizeDelta = new Vector2(0f, 84f);

            // Sign: wooden plank with the menu's shop icon and the word SHOP.
            var titleRect = (RectTransform)container.Find("Title");
            var plank = titleRect.GetComponent<Image>();
            plank.sprite = p.Rounded;
            plank.type = Image.Type.Sliced;
            plank.color = Wood;
            plank.preserveAspect = false;
            titleRect.anchoredPosition = new Vector2(0f, 16f);
            titleRect.sizeDelta = new Vector2(400f, 104f);
            ClearChildren(titleRect);

            Image signIcon = NewImage("icon", titleRect, p.ShopIcon, Color.white);
            signIcon.preserveAspect = true;
            signIcon.rectTransform.anchorMin = new Vector2(0f, 0f);
            signIcon.rectTransform.anchorMax = new Vector2(0f, 1f);
            signIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            signIcon.rectTransform.anchoredPosition = new Vector2(20f, 0f);
            signIcon.rectTransform.sizeDelta = new Vector2(90f, -14f);

            TMP_Text signText = NewText("label", titleRect, 72f, new Color(1f, 0.93f, 0.74f));
            signText.rectTransform.anchorMin = Vector2.zero;
            signText.rectTransform.anchorMax = Vector2.one;
            signText.rectTransform.offsetMin = new Vector2(110f, 0f);
            signText.rectTransform.offsetMax = new Vector2(-12f, 0f);
            signText.text = "SHOP";

            // Tabs sit under the awning, and the body keeps a strip of frame visible around it.
            var tabsRect = (RectTransform)container.Find("Tabs");
            tabsRect.anchoredPosition = new Vector2(0f, -70f);
            tabsRect.sizeDelta = new Vector2(0f, 80f);
            tabsRect.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            var body = (RectTransform)container.Find("Body");
            body.offsetMin = new Vector2(20f, 26f);
            body.offsetMax = new Vector2(-20f, -152f);
            var bodyImage = body.GetComponent<Image>();
            bodyImage.sprite = p.Rounded;
            bodyImage.type = Image.Type.Sliced;
            bodyImage.color = BodyCream;

            // Wallet: coin balance pill.
            var wallet = (RectTransform)shopRoot.Find("Wallet_Coin");
            var walletImage = wallet.GetComponent<Image>();
            walletImage.sprite = p.Rounded;
            walletImage.type = Image.Type.Sliced;
            walletImage.color = Navy;
            wallet.sizeDelta = new Vector2(280f, 80f);

            var coin = wallet.Find("Image").GetComponent<Image>();
            coin.sprite = p.Coin;
            coin.preserveAspect = true;
            coin.rectTransform.anchorMin = new Vector2(0f, 0f);
            coin.rectTransform.anchorMax = new Vector2(0f, 1f);
            coin.rectTransform.pivot = new Vector2(0f, 0.5f);
            coin.rectTransform.anchoredPosition = new Vector2(10f, 0f);
            coin.rectTransform.sizeDelta = new Vector2(64f, -14f);

            var amount = wallet.Find("Text (TMP)").GetComponent<TMP_Text>();
            amount.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            amount.fontSize = 44f;
            amount.color = Color.white;
            amount.alignment = TextAlignmentOptions.Right;
            amount.rectTransform.anchorMin = Vector2.zero;
            amount.rectTransform.anchorMax = Vector2.one;
            amount.rectTransform.offsetMin = new Vector2(82f, 0f);
            amount.rectTransform.offsetMax = new Vector2(-22f, 0f);
            amount.text = "0";
        }

        private static ShopTabButton[] BuildTabs(Transform tabs, GameObject tabPrefab)
        {
            ClearChildren(tabs);

            var row = GetOrAdd<HorizontalLayoutGroup>(tabs.gameObject);
            row.padding = new RectOffset(30, 30, 0, 0);
            row.spacing = 12f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            return new[]
            {
                NewTab(tabPrefab, tabs, "Bundles", Art("tab_bundles")),
                NewTab(tabPrefab, tabs, "Characters", Art("tab_characters")),
                NewTab(tabPrefab, tabs, "Floaties", Art("tab_floaties")),
                NewTab(tabPrefab, tabs, "Weapons", Art("tab_weapons")),
            };
        }

        private static ShopTabButton NewTab(GameObject prefab, Transform parent, string label, Sprite icon)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = $"Tab_{label}";

            var text = instance.transform.Find("label").GetComponent<TMP_Text>();
            text.text = label;
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);

            var image = instance.transform.Find("icon").GetComponent<Image>();
            image.sprite = icon;
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);

            return instance.GetComponent<ShopTabButton>();
        }

        private static ShopPanel BuildBundlesPanel(Transform body, Palette p, ShopCoinPackCard coinPack, ShopBundleCard bundle)
        {
            DestroyChild(body, "Panel_Bundles");

            RectTransform panel = NewPanel(body, "Panel_Bundles");
            ScrollRect scroll = NewScrollView(panel, horizontal: true);

            RectTransform content = scroll.content;
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 5f;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            NewSectionHeader(content, "Coin Packs", p);
            Transform coinRow = NewCardRow(content, "Row_CoinPacks");
            NewSectionHeader(content, "Bundles", p);
            Transform bundleRow = NewCardRow(content, "Row_Bundles");

            Button left = NewArrow(panel, "Btn_Left", p.ArrowLeft, 0f, -46f);
            Button right = NewArrow(panel, "Btn_Right", p.ArrowRight, 1f, 46f);

            var view = panel.gameObject.AddComponent<ShopBundlesPanel>();
            SetRef(view, "m_CoinPackPrefab", coinPack);
            SetRef(view, "m_CoinPackRow", coinRow);
            SetRef(view, "m_BundlePrefab", bundle);
            SetRef(view, "m_BundleRow", bundleRow);
            SetRef(view, "m_ScrollRect", scroll);
            SetRef(view, "m_LeftButton", left);
            SetRef(view, "m_RightButton", right);
            return view;
        }

        private static ShopPanel BuildItemsPanel(Transform body, string name, ShopCategory category, ShopItemCard card)
        {
            DestroyChild(body, name);

            RectTransform panel = NewPanel(body, name);
            ScrollRect scroll = NewScrollView(panel, horizontal: false);

            RectTransform content = scroll.content;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var view = panel.gameObject.AddComponent<ShopItemsPanel>();
            var so = new SerializedObject(view);
            so.FindProperty("m_Category").enumValueIndex = (int)category;
            so.FindProperty("m_CardPrefab").objectReferenceValue = card;
            so.FindProperty("m_Grid").objectReferenceValue = grid;
            so.FindProperty("m_ScrollRect").objectReferenceValue = scroll;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.gameObject.SetActive(false);
            return view;
        }

        private static RectTransform NewPanel(Transform body, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(body, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>A ScrollRect inset from the panel edges, leaving room for the close button at the bottom.</summary>
        private static ScrollRect NewScrollView(RectTransform panel, bool horizontal)
        {
            var go = new GameObject("Scroll", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(panel, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 38f);
            rect.offsetMax = new Vector2(-24f, -8f);

            // Fully transparent, but it catches drags that land between cards.
            Image hit = go.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            var viewport = (RectTransform)viewportGo.transform;
            viewport.SetParent(rect, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var content = (RectTransform)contentGo.transform;
            content.SetParent(viewport, false);

            var scroll = go.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            return scroll;
        }

        private static void NewSectionHeader(Transform parent, string text, Palette p)
        {
            Image ribbon = NewImage($"Header_{text.Replace(" ", string.Empty)}", parent, p.Rounded, Ribbon);
            ribbon.type = Image.Type.Sliced;
            var layout = ribbon.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 230f;
            layout.minHeight = 36f;
            layout.preferredHeight = 36f;

            TMP_Text label = NewText("label", ribbon.transform, 25f, Color.white);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.text = text;
        }

        private static Transform NewCardRow(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 14f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            go.AddComponent<LayoutElement>().flexibleHeight = 1f;
            return go.transform;
        }

        private static Button NewArrow(Transform panel, string name, Sprite sprite, float anchorX, float x)
        {
            Image image = NewImage(name, panel, sprite, Color.white);
            image.preserveAspect = true;
            image.raycastTarget = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(84f, 110f);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Button BuildRestoreButton(Transform container)
        {
            DestroyChild(container, "Btn_Restore");

            TMP_Text label = NewText("Btn_Restore", container, 24f, Ink);
            label.raycastTarget = true;
            label.text = "Restore Purchases";
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(48f, 30f);
            rect.sizeDelta = new Vector2(260f, 36f);

            Button button = label.gameObject.AddComponent<Button>();
            button.targetGraphic = label;
            return button;
        }

        private static void BuildStatus(Transform shopRoot, Palette p, out CanvasGroup group, out TMP_Text text)
        {
            DestroyChild(shopRoot, "Status");

            Image pill = NewImage("Status", shopRoot, p.Rounded, Navy);
            pill.type = Image.Type.Sliced;
            RectTransform rect = pill.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 110f);
            rect.sizeDelta = new Vector2(760f, 68f);

            text = NewText("label", pill.transform, 32f, Color.white);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;

            group = pill.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private static void WireBootstrap()
        {
            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);
            var bootstrap = Object.FindFirstObjectByType<Bootstrap>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                Debug.LogError("[ShopWiring] No Bootstrap component in Bootstrap.unity.");
            }
            else
            {
                SetRef(bootstrap, "m_ShopCatalog", AssetDatabase.LoadAssetAtPath<ShopCatalogSO>(CatalogPath));
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.CloseScene(scene, true);
        }

        private static Transform FindFlow(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Canvas")
                    return root.transform.Find("GameFlowUI");

            throw new System.InvalidOperationException("[ShopWiring] Menu.unity has no Canvas/GameFlowUI.");
        }

#endregion

#region Helpers

        private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text NewText(string name, Transform parent, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void AnchorTop(RectTransform rect, float top, float height, float inset)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-2f * inset, height);
        }

        private static void AnchorBottom(RectTransform rect, float bottom, float height, float inset)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(-2f * inset, height);
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void SetRef(Component component, string field, Object value)
        {
            if (value == null) Debug.LogWarning($"[ShopWiring] {component.GetType().Name}.{field} set to null (missing target).");
            var so = new SerializedObject(component);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[ShopWiring] {component.GetType().Name} has no serialized field '{field}'.");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void DestroyChild(Transform parent, string name)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                if (parent.GetChild(i).name == name)
                    Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void EnsureFolder(string folder)
        {
            if (Directory.Exists(folder)) return;
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

#endregion
    }
}
