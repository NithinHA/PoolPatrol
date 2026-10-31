using System.Collections.Generic;
using Loadout;
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
    /// Sets up coin / arena-clear unlockables (Docs/economy-and-progression.md → Unlockables):
    /// assigns each catalog item its unlock rule, gives the loadout thumbnail prefab a lock overlay
    /// and price tag, and builds the coin balance + purchase panel on the Menu scene's Loadout screen.
    ///
    /// Re-runnable: unlock data is overwritten from the table below, and every object it builds is
    /// deleted and rebuilt by name. Items not in the table become Free.
    /// </summary>
    public static class LoadoutUnlockWiring
    {
        private const string MenuScenePath  = "Assets/Scenes/Menu.unity";
        private const string ItemPrefabPath = "Assets/Prefabs/UI/Menu/LoadoutScreen/LoadoutSelectionItem.prefab";
        private const string LevelCardPath  = "Assets/Prefabs/UI/Menu/LevelScreen/LevelCard.prefab";
        private const string FontPath       = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private static readonly Color CoinGold = new(1f, 0.78f, 0.15f);
        private static readonly Color InkColor = new(0.12f, 0.18f, 0.3f);

        private readonly struct Rule
        {
            public readonly UnlockType Type;
            public readonly int Price;
            public readonly int Arena;

            public Rule(UnlockType type, int price = 0, int arena = 0)
            {
                Type = type;
                Price = price;
                Arena = arena;
            }
        }

        /// <summary>
        /// Starting content. Per the doc: each arena completion unlocks one head or floatie, the rest
        /// are coin-priced on the ~300 / 800 / 1500 tiers. Weapons stay free for now (only two exist).
        /// </summary>
        private static readonly Dictionary<string, Rule> Rules = new()
        {
            { "char_surfer",      new Rule(UnlockType.ArenaClear, arena: 0) },
            { "char_goggles",     new Rule(UnlockType.Coins, 300) },
            { "char_captain",     new Rule(UnlockType.Coins, 800) },
            { "floatie_flamingo", new Rule(UnlockType.Coins, 800) },
            { "floatie_unicorn",  new Rule(UnlockType.Coins, 1500) },
        };

        [MenuItem("PoolPatrol/Loadout/Wire Coin Unlocks")]
        public static void WireAll()
        {
            ApplyUnlockRules();
            Sprite lockSprite = FindLockSprite();
            WireItemPrefab(lockSprite);
            WireMenuScene();

            AssetDatabase.SaveAssets();
            Debug.Log("[LoadoutUnlockWiring] Coin unlocks wired.");
        }

#region Content

        private static void ApplyUnlockRules()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:LoadoutCatalogSO"))
            {
                var catalog = AssetDatabase.LoadAssetAtPath<LoadoutCatalogSO>(AssetDatabase.GUIDToAssetPath(guid));
                var items = new List<LoadoutItemSO>();
                items.AddRange(catalog.Characters);
                items.AddRange(catalog.Floaties);
                items.AddRange(catalog.Weapons);

                foreach (LoadoutItemSO item in items)
                {
                    if (item == null) continue;

                    Rule rule = Rules.TryGetValue(item.Id ?? string.Empty, out Rule found) ? found : new Rule(UnlockType.Free);
                    item.Unlock = rule.Type;
                    item.CoinPrice = rule.Type == UnlockType.Coins ? rule.Price : item.CoinPrice;
                    item.ArenaIndex = rule.Arena;
                    EditorUtility.SetDirty(item);
                }
            }
        }

#endregion

#region Thumbnail prefab

        private static Sprite FindLockSprite()
        {
            var card = AssetDatabase.LoadAssetAtPath<GameObject>(LevelCardPath);
            Transform lockTransform = card != null ? card.transform.Find("Locked/icon_lock") : null;
            if (lockTransform == null || !lockTransform.TryGetComponent(out Image lockImage))
                return null;
            return lockImage.sprite;
        }

        private static void WireItemPrefab(Sprite lockSprite)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(ItemPrefabPath);
            var item = contents.GetComponent<LoadoutSelectionItem>();

            DestroyChild(contents.transform, "locked");

            // Dimmed overlay with a small lock and a price / reason strip along the bottom.
            Image overlay = NewImage("locked", contents.transform, null, new Color(0f, 0f, 0f, 0.35f));
            Stretch(overlay.rectTransform);

            if (lockSprite != null)
            {
                Image lockIcon = NewImage("lock_icon", overlay.transform, lockSprite, Color.white);
                lockIcon.preserveAspect = true;
                SetAnchors(lockIcon.rectTransform, new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.85f));
            }

            Image strip = NewImage("tag_bg", overlay.transform, null, new Color(0f, 0f, 0f, 0.6f));
            SetAnchors(strip.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.32f));

            TMP_Text tag = NewText("tag", strip.transform, 24f, CoinGold);
            Stretch(tag.rectTransform);
            tag.enableAutoSizing = true;
            tag.fontSizeMin = 12f;
            tag.fontSizeMax = 26f;
            tag.text = "300";

            overlay.gameObject.SetActive(false);

            var so = new SerializedObject(item);
            so.FindProperty("m_Locked").objectReferenceValue = overlay.gameObject;
            so.FindProperty("m_TagText").objectReferenceValue = tag;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(contents, ItemPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

#endregion

#region Menu scene

        private static void WireMenuScene()
        {
            Scene previous = SceneManager.GetActiveScene();
            string previousPath = previous.path;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            var screen = Object.FindFirstObjectByType<LoadoutScreen>(FindObjectsInactive.Include);
            if (screen == null)
            {
                Debug.LogError("[LoadoutUnlockWiring] No LoadoutScreen in the Menu scene.");
                return;
            }

            TMP_Text balance = BuildCoinBalance(screen.transform);
            LoadoutPurchasePanel panel = BuildPurchasePanel(screen.transform);

            var so = new SerializedObject(screen);
            so.FindProperty("m_CoinBalanceText").objectReferenceValue = balance;
            so.FindProperty("m_PurchasePanel").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // Leave the editor on the scene it was on.
            if (!string.IsNullOrEmpty(previousPath) && previousPath != MenuScenePath)
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }

        /// <summary>Coin icon + count in the screen's top-right corner, above the weapon card.</summary>
        private static TMP_Text BuildCoinBalance(Transform screen)
        {
            DestroyChild(screen, "coin_balance");

            Image bg = NewImage("coin_balance", screen, BuiltinSprite("UI/Skin/UISprite.psd"), new Color(0f, 0f, 0f, 0.35f));
            bg.type = Image.Type.Sliced;
            RectTransform rect = bg.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-60f, -20f);
            rect.sizeDelta = new Vector2(320f, 100f);

            Image coin = NewImage("coin_icon", bg.transform, BuiltinSprite("UI/Skin/Knob.psd"), CoinGold);
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            coin.rectTransform.pivot = new Vector2(0f, 0.5f);
            coin.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            coin.rectTransform.sizeDelta = new Vector2(70f, 70f);

            TMP_Text count = NewText("count", bg.transform, 56f, Color.white);
            count.alignment = TextAlignmentOptions.Right;
            SetAnchors(count.rectTransform, Vector2.zero, Vector2.one);
            count.rectTransform.offsetMin = new Vector2(100f, 0f);
            count.rectTransform.offsetMax = new Vector2(-24f, 0f);
            count.text = "0";
            return count;
        }

        /// <summary>
        /// The panel component sits on an always-active holder so its Awake runs at scene load; the
        /// dimmer it toggles is a child.
        /// </summary>
        private static LoadoutPurchasePanel BuildPurchasePanel(Transform screen)
        {
            DestroyChild(screen, "purchase_panel");

            var holder = new GameObject("purchase_panel", typeof(RectTransform));
            holder.transform.SetParent(screen, false);
            Stretch((RectTransform)holder.transform);
            holder.transform.SetAsLastSibling();

            Image dimmer = NewImage("root", holder.transform, null, new Color(0f, 0f, 0f, 0.6f));
            dimmer.raycastTarget = true;
            Stretch(dimmer.rectTransform);
            Button dimmerButton = dimmer.gameObject.AddComponent<Button>();
            dimmerButton.transition = Selectable.Transition.None;

            Image box = NewImage("box", dimmer.transform, BuiltinSprite("UI/Skin/UISprite.psd"), new Color(0.96f, 0.97f, 1f));
            box.type = Image.Type.Sliced;
            box.raycastTarget = true;   // taps on the box must not fall through to the dimmer's close
            RectTransform boxRect = box.rectTransform;
            boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(760f, 620f);

            Image icon = NewImage("icon", box.transform, null, Color.white);
            icon.preserveAspect = true;
            Center(icon.rectTransform, new Vector2(0f, 150f), new Vector2(220f, 220f));

            TMP_Text title = NewText("name_text", box.transform, 58f, InkColor);
            Center(title.rectTransform, new Vector2(0f, 0f), new Vector2(700f, 70f));

            TMP_Text reason = NewText("reason_text", box.transform, 36f, InkColor);
            reason.textWrappingMode = TextWrappingModes.Normal;
            Center(reason.rectTransform, new Vector2(0f, -90f), new Vector2(680f, 110f));

            // Buttons reuse the screen's own styled buttons so the panel matches the menu art.
            Transform play = screen.Find("Btn_Play");
            Button buy = CloneButton(play, box.transform, "Btn_Buy", new Vector2(0f, -225f), new Vector2(460f, 115f));
            TMP_Text buyLabel = buy != null ? buy.GetComponentInChildren<TMP_Text>(true) : null;
            if (buyLabel != null) buyLabel.text = "Buy";

            Transform back = screen.Find("TopLeft/Btn_Back");
            Button close = CloneButton(back, box.transform, "Btn_Close", new Vector2(345f, 275f), new Vector2(100f, 100f));

            var panel = holder.AddComponent<LoadoutPurchasePanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("m_Root").objectReferenceValue = dimmer.gameObject;
            so.FindProperty("m_Box").objectReferenceValue = boxRect;
            so.FindProperty("m_Icon").objectReferenceValue = icon;
            so.FindProperty("m_NameText").objectReferenceValue = title;
            so.FindProperty("m_ReasonText").objectReferenceValue = reason;
            so.FindProperty("m_BuyButton").objectReferenceValue = buy;
            so.FindProperty("m_BuyLabel").objectReferenceValue = buyLabel;
            SerializedProperty closes = so.FindProperty("m_CloseButtons");
            closes.arraySize = close != null ? 2 : 1;
            closes.GetArrayElementAtIndex(0).objectReferenceValue = dimmerButton;
            if (close != null) closes.GetArrayElementAtIndex(1).objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();

            dimmer.gameObject.SetActive(false);
            return panel;
        }

        private static Button CloneButton(Transform source, Transform parent, string name, Vector2 position, Vector2 size)
        {
            if (source == null)
            {
                Debug.LogWarning($"[LoadoutUnlockWiring] No source button for {name}; skipped.");
                return null;
            }

            GameObject clone = Object.Instantiate(source.gameObject, parent);
            clone.name = name;
            var button = clone.GetComponent<Button>();
            // Drop any inspector-wired calls copied from the original (Play / Back).
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
            Center((RectTransform)clone.transform, position, size);
            return button;
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
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static Sprite BuiltinSprite(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        private static void Stretch(RectTransform rect) => SetAnchors(rect, Vector2.zero, Vector2.one);

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                if (parent.GetChild(i).name == name)
                    Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

#endregion
    }
}
