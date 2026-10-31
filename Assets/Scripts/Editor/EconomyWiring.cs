using System.Collections.Generic;
using Economy;
using Economy.UI;
using Enemy;
using Enemy.Death;
using Pooling;
using TMPro;
using UI.InGame;
using UI.Menu;
using UI.PostGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// Sets up the score + coin economy (Docs/economy-and-progression.md): the EconomyConfig asset,
    /// the pooled score bubble and coin pouch prefabs, per-enemy base score values, the gem
    /// dropper's coin pouch drop, and the Game scene / level card wiring (ScoreKeeper, pools, coin
    /// wallet HUD, post-game stats, pause-menu score row, level best score/streak).
    ///
    /// Re-runnable: assets are reused when they exist, and every scene/prefab object it builds is
    /// deleted and rebuilt by name.
    ///
    /// The coin art is a placeholder (Unity's built-in round "Knob" sprite tinted gold) until a real
    /// coin / pouch sprite exists.
    /// </summary>
    public static class EconomyWiring
    {
        private const string GameScenePath   = "Assets/Scenes/Game.unity";
        private const string ConfigPath      = "Assets/ScriptableObjects/Economy/EconomyConfig.asset";
        private const string BubblePath      = "Assets/Prefabs/Score/ScoreBubble.prefab";
        private const string GemPrefabPath   = "Assets/Prefabs/DroppedItems/gem_blue.prefab";
        private const string PouchPath       = "Assets/Prefabs/DroppedItems/coin_pouch.prefab";
        private const string LevelCardPath   = "Assets/Prefabs/UI/Menu/LevelScreen/LevelCard.prefab";
        private const string BubbleFontPath  = "Assets/Feel/MMTools/Tools/MMTween/Demo/Fonts/Lato/SDF/Lato-Black SDF.asset";
        private const string GemDropperPath  = "Assets/Prefabs/Enemies/RubberDuck_gemDropper.prefab";

        private static readonly Color CoinGold = new(1f, 0.78f, 0.15f);

        /// <summary>Base score per enemy prefab (doc: idle & droppers 10, random 15, chaser 20, shooter 25).</summary>
        private static readonly Dictionary<string, int> EnemyScoreValues = new()
        {
            { "Assets/Prefabs/Enemies/RubberDuck_idle.prefab",         10 },
            { "Assets/Prefabs/Enemies/RubberDuck_gemDropper.prefab",   10 },
            { "Assets/Prefabs/Enemies/RubberDuck_heartDropper.prefab", 10 },
            { "Assets/Prefabs/Enemies/RubberDuck_randomMove.prefab",   15 },
            { "Assets/Prefabs/Enemies/RubberDuck_chase.prefab",        20 },
            { "Assets/Prefabs/Enemies/RubberDuck_shooter.prefab",      25 },
        };

        [MenuItem("PoolPatrol/Economy/Wire Score & Coins")]
        public static void WireAll()
        {
            EconomyConfigSO config = EnsureConfig();
            ScoreBubble bubble     = EnsureScoreBubblePrefab();
            CoinCollectable pouch  = EnsureCoinPouchPrefab();
            if (bubble == null || pouch == null)
                return;

            SetEnemyScoreValues();
            AddCoinPouchDropToGemDropper();
            WireLevelCard();
            WireGameScene(config, bubble, pouch);

            AssetDatabase.SaveAssets();
            Debug.Log("[EconomyWiring] Score & coin economy wired.");
        }

#region Assets

        private static EconomyConfigSO EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<EconomyConfigSO>(ConfigPath);
            if (config != null)
                return config;

            EnsureFolder(System.IO.Path.GetDirectoryName(ConfigPath));
            config = ScriptableObject.CreateInstance<EconomyConfigSO>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static ScoreBubble EnsureScoreBubblePrefab()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BubbleFontPath);
            if (font == null)
                font = TMP_Settings.defaultFontAsset;

            var root = new GameObject("ScoreBubble");
            var textGo = new GameObject("text", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);

            var text = textGo.AddComponent<TextMeshPro>();
            text.font = font;
            text.fontSharedMaterial = EnsureBubbleMaterial(font);
            text.fontSize = 11f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.text = "132";
            text.rectTransform.sizeDelta = new Vector2(4f, 1.5f);

            var renderer = textGo.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "Foreground";
            renderer.sortingOrder = 50;

            var bubble = root.AddComponent<ScoreBubble>();
            var so = new SerializedObject(bubble);
            so.FindProperty("m_Text").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(System.IO.Path.GetDirectoryName(BubblePath));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BubblePath);
            Object.DestroyImmediate(root);
            return prefab != null ? prefab.GetComponent<ScoreBubble>() : null;
        }

        /// <summary>
        /// A dark outline keeps the bubble readable over the bright water whatever its tier colour.
        /// Its own material preset, so the outline doesn't leak onto every other text using the font.
        /// </summary>
        private static Material EnsureBubbleMaterial(TMP_FontAsset font)
        {
            string path = System.IO.Path.GetDirectoryName(BubblePath)?.Replace('\\', '/') + "/ScoreBubble_Outline.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                EnsureFolder(System.IO.Path.GetDirectoryName(path));
                material = new Material(font.material);
                AssetDatabase.CreateAsset(material, path);
            }

            material.EnableKeyword("OUTLINE_ON");
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.05f, 0.1f, 0.2f, 1f));
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Builds the pouch from the gem prefab so it floats, homes, sinks and splashes exactly like a
        /// gem, then swaps the reward and the art.
        /// </summary>
        private static CoinCollectable EnsureCoinPouchPrefab()
        {
            var gemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GemPrefabPath);
            if (gemPrefab == null)
            {
                Debug.LogError($"[EconomyWiring] Gem prefab not found at {GemPrefabPath}.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(gemPrefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "coin_pouch";

            var gem = instance.GetComponent<GemCollectable>();
            var gemSo = new SerializedObject(gem);

            var coin = instance.AddComponent<CoinCollectable>();
            var coinSo = new SerializedObject(coin);
            foreach (string field in new[]
                     {
                         "m_CollectionRadius", "m_GrantRadius", "m_HomingSpeed", "m_FlyToUiDuration",
                         "m_FlyToUiEase", "m_IdleLifetime", "m_SinkWarningTime", "m_SinkDuration",
                         "m_SinkDepth", "m_SinkScale", "m_Gfx",
                     })
                coinSo.CopyFromSerializedProperty(gemSo.FindProperty(field));
            coinSo.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(gem);

            // Placeholder art: a gold disc. Drop the crystal animator so it can't swap the sprite back.
            Transform gfx = instance.transform.Find("gfx");
            if (gfx != null)
            {
                var animator = gfx.GetComponent<Animator>();
                if (animator != null) Object.DestroyImmediate(animator);

                var sprite = gfx.GetComponent<SpriteRenderer>();
                sprite.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                sprite.color = CoinGold;
                gfx.localScale = Vector3.one * 3f;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PouchPath);
            Object.DestroyImmediate(instance);
            return prefab != null ? prefab.GetComponent<CoinCollectable>() : null;
        }

        private static void SetEnemyScoreValues()
        {
            foreach (KeyValuePair<string, int> pair in EnemyScoreValues)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pair.Key);
                EnemyController enemy = prefab != null ? prefab.GetComponent<EnemyController>() : null;
                if (enemy == null)
                {
                    Debug.LogWarning($"[EconomyWiring] No EnemyController prefab at {pair.Key}; skipped.");
                    continue;
                }

                var so = new SerializedObject(enemy);
                so.FindProperty(nameof(EnemyController.ScoreValue)).intValue = pair.Value;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab);
            }
        }

        private static void AddCoinPouchDropToGemDropper()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(GemDropperPath);
            if (contents.GetComponent<DropCoinPouchOnDeath>() == null)
                contents.AddComponent<DropCoinPouchOnDeath>();
            PrefabUtility.SaveAsPrefabAsset(contents, GemDropperPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static void WireLevelCard()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(LevelCardPath);
            var card = contents.GetComponent<LevelCard>();
            if (card == null)
            {
                Debug.LogError("[EconomyWiring] LevelCard prefab has no LevelCard component.");
                PrefabUtility.UnloadPrefabContents(contents);
                return;
            }

            TMP_Text reference = contents.transform.Find("level_index_text")?.GetComponent<TMP_Text>();
            TMP_Text best   = NewText("best_score_text", contents.transform, reference, 26f, TextAlignmentOptions.Left);
            TMP_Text streak = NewText("best_streak_text", contents.transform, reference, 26f, TextAlignmentOptions.Right);

            // Bottom strip of the card, under the level number: best score left, best streak right.
            Place(best.rectTransform,   new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(24f, 14f), new Vector2(-4f, 44f));
            Place(streak.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(4f, 14f), new Vector2(-24f, 44f));
            best.text = "Best 0";
            streak.text = "Streak 0";
            best.gameObject.SetActive(false);
            streak.gameObject.SetActive(false);

            // Keep them under the lock overlay.
            Transform locked = contents.transform.Find("Locked");
            if (locked != null)
                locked.SetAsLastSibling();

            var so = new SerializedObject(card);
            so.FindProperty("m_BestScoreText").objectReferenceValue = best;
            so.FindProperty("m_BestStreakText").objectReferenceValue = streak;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(contents, LevelCardPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

#endregion

#region Game scene

        private static void WireGameScene(EconomyConfigSO config, ScoreBubble bubble, CoinCollectable pouch)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != GameScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            }

            WirePools(bubble, pouch);
            WireLevelManager(config);
            WireCoinWallet();
            WirePostGame();
            WirePauseMenuScore();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WirePools(ScoreBubble bubble, CoinCollectable pouch)
        {
            var pools = Object.FindFirstObjectByType<ObjectPoolManager>();
            if (pools == null)
            {
                Debug.LogError("[EconomyWiring] No ObjectPoolManager in the Game scene.");
                return;
            }

            var so = new SerializedObject(pools);
            SerializedProperty list = so.FindProperty("m_PoolConfigs");
            SetPool(list, PoolableItemType.DroppedCoinPouch, pouch.gameObject, 3, 10);
            SetPool(list, PoolableItemType.ScoreBubble, bubble.gameObject, 10, 40);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPool(SerializedProperty list, PoolableItemType type, GameObject prefab, int capacity, int max)
        {
            SerializedProperty entry = null;
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty candidate = list.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("Type").intValue == (int)type)
                {
                    entry = candidate;
                    break;
                }
            }

            if (entry == null)
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                entry = list.GetArrayElementAtIndex(list.arraySize - 1);
            }

            entry.FindPropertyRelative("Type").intValue = (int)type;
            entry.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("DefaultCapacity").intValue = capacity;
            entry.FindPropertyRelative("MaxSize").intValue = max;
        }

        private static void WireLevelManager(EconomyConfigSO config)
        {
            var levelManager = Object.FindFirstObjectByType<LevelManager>();
            if (levelManager == null)
            {
                Debug.LogError("[EconomyWiring] No LevelManager in the Game scene.");
                return;
            }

            var lmSo = new SerializedObject(levelManager);
            lmSo.FindProperty("m_EconomyConfig").objectReferenceValue = config;
            lmSo.ApplyModifiedPropertiesWithoutUndo();

            var keeper = levelManager.GetComponent<ScoreKeeper>();
            if (keeper == null)
                keeper = levelManager.gameObject.AddComponent<ScoreKeeper>();

            var keeperSo = new SerializedObject(keeper);
            keeperSo.FindProperty("m_Config").objectReferenceValue = config;
            keeperSo.FindProperty("m_WeaponController").objectReferenceValue =
                Object.FindFirstObjectByType<Weapon.PlayerWeaponController>(FindObjectsInactive.Include);
            keeperSo.FindProperty("m_PlayerController").objectReferenceValue =
                Object.FindFirstObjectByType<Player.PlayerController>(FindObjectsInactive.Include);
            keeperSo.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Clones the gem wallet into a coin wallet that waits above it. The clone keeps the gem
        /// wallet's look; only its behaviour, icon and layout participation change.
        /// </summary>
        private static void WireCoinWallet()
        {
            var gemWallet = Object.FindFirstObjectByType<GemWalletUI>(FindObjectsInactive.Include);
            if (gemWallet == null)
            {
                Debug.LogError("[EconomyWiring] No GemWalletUI in the Game scene.");
                return;
            }

            var slot = (RectTransform)gemWallet.transform;
            Transform parent = slot.parent;
            DestroyChild(parent, "CoinWallet");

            var slotGroup = slot.GetComponent<CanvasGroup>();
            if (slotGroup == null)
                slotGroup = slot.gameObject.AddComponent<CanvasGroup>();

            GameObject coinWallet = Object.Instantiate(slot.gameObject, parent);
            coinWallet.name = "CoinWallet";
            Object.DestroyImmediate(coinWallet.GetComponent<GemWalletUI>());
            Object.DestroyImmediate(coinWallet.GetComponent<CollectionTargetMarker>());
            Object.DestroyImmediate(coinWallet.GetComponent<CanvasGroup>());
            coinWallet.transform.SetAsLastSibling();   // draws over the pause button while sliding

            var layout = coinWallet.GetComponent<LayoutElement>();
            if (layout == null)
                layout = coinWallet.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            Image icon = coinWallet.transform.Find("bg/gem_icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.name = "coin_icon";
                icon.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                icon.color = CoinGold;
                icon.preserveAspect = true;
            }

            TMP_Text count = coinWallet.transform.Find("label/text")?.GetComponent<TMP_Text>();

            var ui = coinWallet.AddComponent<CoinWalletUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("m_Panel").objectReferenceValue = coinWallet.transform;
            so.FindProperty("m_CountText").objectReferenceValue = count;
            so.FindProperty("m_Slot").objectReferenceValue = slot;
            so.FindProperty("m_SlotGroup").objectReferenceValue = slotGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Parked off-screen in edit mode too, so it never shows in the scene view's HUD layout.
            ((RectTransform)coinWallet.transform).anchoredPosition = slot.anchoredPosition + Vector2.up * 450f;
        }

        private static void WirePostGame()
        {
            var postGame = Object.FindFirstObjectByType<PostGameUI>(FindObjectsInactive.Include);
            if (postGame == null)
            {
                Debug.LogError("[EconomyWiring] No PostGameUI in the Game scene.");
                return;
            }

            var so = new SerializedObject(postGame);
            var title = (TMP_Text)so.FindProperty("m_TitleText").objectReferenceValue;
            Transform container = title != null ? title.transform.parent : postGame.transform;

            foreach (string name in new[] { "ScoreText", "StreakText", "CoinsText", "CoinBreakdownText" })
                DestroyChild(container, name);

            // Make room between the title and the buttons for the stats block.
            if (title != null)
                title.rectTransform.anchoredPosition = new Vector2(0f, 290f);
            LowerButton((Button)so.FindProperty("m_RetryButton").objectReferenceValue);
            LowerButton((Button)so.FindProperty("m_MainMenuButton").objectReferenceValue);

            TMP_Text score     = NewText("ScoreText", container, title, 54f, TextAlignmentOptions.Center);
            TMP_Text streak    = NewText("StreakText", container, title, 40f, TextAlignmentOptions.Center);
            TMP_Text coins     = NewText("CoinsText", container, title, 54f, TextAlignmentOptions.Center);
            TMP_Text breakdown = NewText("CoinBreakdownText", container, title, 28f, TextAlignmentOptions.Top);

            PlaceCentered(score.rectTransform,     new Vector2(0f, 190f), new Vector2(1000f, 70f));
            PlaceCentered(streak.rectTransform,    new Vector2(0f, 130f), new Vector2(1000f, 56f));
            PlaceCentered(coins.rectTransform,     new Vector2(0f, 65f),  new Vector2(1000f, 70f));
            // Up to five breakdown lines, growing downward toward the (lowered) buttons.
            breakdown.rectTransform.anchorMin = breakdown.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            breakdown.rectTransform.pivot = new Vector2(0.5f, 1f);
            breakdown.rectTransform.anchoredPosition = new Vector2(0f, 25f);
            breakdown.rectTransform.sizeDelta = new Vector2(1000f, 170f);
            coins.color = CoinGold;

            score.text = "Score  0";
            streak.text = "Highest Streak  0";
            coins.text = "Coins Earned  +0";
            breakdown.text = "";

            so.FindProperty("m_ScoreText").objectReferenceValue = score;
            so.FindProperty("m_StreakText").objectReferenceValue = streak;
            so.FindProperty("m_CoinsText").objectReferenceValue = coins;
            so.FindProperty("m_CoinBreakdownText").objectReferenceValue = breakdown;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Clones the pause menu's "enemies killed" row into a "score" row right below it.</summary>
        private static void WirePauseMenuScore()
        {
            var pause = Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
            if (pause == null)
                return;

            var so = new SerializedObject(pause);
            var killedValue = (TMP_Text)so.FindProperty("m_EnemiesKilledValueText").objectReferenceValue;
            if (killedValue == null || killedValue.transform.parent == null)
            {
                Debug.LogWarning("[EconomyWiring] Pause menu has no enemies-killed row to clone; score row skipped.");
                return;
            }

            Transform row = killedValue.transform.parent;
            DestroyChild(row.parent, "item4_score");

            GameObject scoreRow = Object.Instantiate(row.gameObject, row.parent);
            scoreRow.name = "item4_score";
            scoreRow.transform.SetSiblingIndex(row.GetSiblingIndex() + 1);

            int valueIndex = killedValue.transform.GetSiblingIndex();
            TMP_Text scoreValue = scoreRow.transform.GetChild(valueIndex).GetComponent<TMP_Text>();
            foreach (TMP_Text text in scoreRow.GetComponentsInChildren<TMP_Text>(true))
                text.text = text == scoreValue ? "0" : "Score";

            so.FindProperty("m_ScoreValueText").objectReferenceValue = scoreValue;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

#endregion

#region Helpers

        private static TMP_Text NewText(string name, Transform parent, TMP_Text style, float size, TextAlignmentOptions alignment)
        {
            DestroyChild(parent, name);

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (style != null)
            {
                text.font = style.font;
                text.color = style.color;
            }
            text.fontSize = size;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Moves a post-game button into the lower band (it is anchor-sized: 12% tall).</summary>
        private static void LowerButton(Button button)
        {
            if (button == null)
                return;

            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(rect.anchorMin.x, 0.18f);
            rect.anchorMax = new Vector2(rect.anchorMax.x, 0.30f);
        }

        private static void PlaceCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                if (parent.GetChild(i).name == name)
                    Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

#endregion
    }
}
