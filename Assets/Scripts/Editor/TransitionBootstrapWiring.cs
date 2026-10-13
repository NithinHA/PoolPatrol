using Loadout;
using PTL.Framework;
using TMPro;
using UI.Transition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// Wires the Game scene's loadout application and the Bootstrap scene's catalog + persistent
    /// transition canvas (loading bar). Driven from the live Editor; see PoolPatrol/Menu.
    /// </summary>
    public static class TransitionBootstrapWiring
    {
        private const string CatalogPath   = "Assets/ScriptableObjects/LoadoutCatalog.asset";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string BootScenePath = "Assets/Scenes/Bootstrap.unity";

        [MenuItem("PoolPatrol/Menu/3. Wire Game Scene (Loadout)")]
        public static void WireGameScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            Transform playerVariants = FindRoot(scene, "Player variants");
            Transform bg = FindRoot(scene, "Bg");
            if (playerVariants == null) { Debug.LogError("[Wiring] 'Player variants' not found in Game scene."); return; }

            GameObject applierGo = GameObject.Find("LoadoutApplier");
            if (applierGo == null) applierGo = new GameObject("LoadoutApplier");
            var applier = GetOrAdd<Loadout.LoadoutApplier>(applierGo);

            SetRef(applier, "m_FloatieBody", playerVariants.Find("DefaultFloatie/GfxParent/Gfx")?.GetComponent<SpriteRenderer>());
            SetRef(applier, "m_FloatieReflection", playerVariants.Find("DefaultFloatie/GfxParent/Reflection")?.GetComponent<SpriteRenderer>());
            SetRef(applier, "m_CharacterHead", playerVariants.Find("Player/Body/Head_sprite")?.GetComponent<SpriteRenderer>());
            var weaponController = playerVariants.Find("Player/WeaponHolder")?.GetComponent<Weapon.PlayerWeaponController>();
            SetRef(applier, "m_WeaponController", weaponController);
            if (bg != null)
                SetRef(applier, "m_PoolRenderer", bg.Find("Background")?.GetComponent<Renderer>());

            // The designer never populated the weapon controller's available-weapons list, so weapon
            // selection had nothing to switch to. Fill it from the weapon children if it's empty.
            if (weaponController != null)
                PopulateAvailableWeapons(weaponController);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Wiring] Game scene LoadoutApplier wired and saved.");
        }

        [MenuItem("PoolPatrol/Menu/4. Wire Bootstrap (Catalog + Transition)")]
        public static void WireBootstrap()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);

            // Catalog → Bootstrap.
            Transform bootstrap = FindRoot(scene, "Bootstrap");
            var catalog = AssetDatabase.LoadAssetAtPath<LoadoutCatalogSO>(CatalogPath);
            if (bootstrap != null && catalog != null)
                SetRef(bootstrap.GetComponent<Bootstrap>(), "m_LoadoutCatalog", catalog);

            // Persistent transition canvas.
            BuildTransitionCanvas(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Wiring] Bootstrap catalog + transition canvas wired and saved.");
        }

        private static void PopulateAvailableWeapons(Weapon.PlayerWeaponController controller)
        {
            var so = new SerializedObject(controller);
            SerializedProperty list = so.FindProperty("m_AvailableWeapons");
            if (list == null) { Debug.LogError("[Wiring] PlayerWeaponController has no m_AvailableWeapons."); return; }
            if (list.arraySize > 0) return;   // already set up by the designer

            Weapon.WeaponBase[] weapons = controller.GetComponentsInChildren<Weapon.WeaponBase>(includeInactive: true);
            list.arraySize = weapons.Length;
            for (int i = 0; i < weapons.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[Wiring] Populated m_AvailableWeapons with {weapons.Length} weapon(s).");
        }

        private static void BuildTransitionCanvas(UnityEngine.SceneManagement.Scene scene)
        {
            if (FindRoot(scene, "TransitionCanvas") != null)
            {
                Debug.Log("[Wiring] TransitionCanvas already exists — skipping build.");
                return;
            }


            var root = new GameObject("TransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var controller = root.AddComponent<SceneTransitionController>();
            controller.DontDestroyOnLoad = true;

            // ── Loading panel ───────────────────────────────────────────
            GameObject loading = NewUI("Loading", root.transform);
            var loadingBg = loading.AddComponent<Image>();
            loadingBg.color = new Color(0.03f, 0.08f, 0.16f, 1f);
            var loadingUi = loading.AddComponent<LoadingScreenUI>();

            GameObject title = NewText("Title", "POOL PATROL", loading.transform, 90, FontStyles.Bold);
            RectTransform titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0.1f, 0.55f); titleRt.anchorMax = new Vector2(0.9f, 0.75f);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;

            GameObject barBg = NewUI("ProgressBG", loading.transform);
            var barBgRt = (RectTransform)barBg.transform;
            barBgRt.anchorMin = new Vector2(0.15f, 0.34f); barBgRt.anchorMax = new Vector2(0.85f, 0.38f);
            barBgRt.offsetMin = Vector2.zero; barBgRt.offsetMax = Vector2.zero;
            var barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(1f, 1f, 1f, 0.15f);

            GameObject fill = NewUI("ProgressFill", barBg.transform);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(1f, 0.82f, 0.2f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
            fillImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject progText = NewText("ProgressText", "0%", loading.transform, 44, FontStyles.Normal);
            RectTransform ptRt = (RectTransform)progText.transform;
            ptRt.anchorMin = new Vector2(0.15f, 0.28f); ptRt.anchorMax = new Vector2(0.85f, 0.33f);
            ptRt.offsetMin = Vector2.zero; ptRt.offsetMax = Vector2.zero;

            SetRef(loadingUi, "m_Root", loading);
            SetRef(loadingUi, "m_ProgressFill", fillImg);
            SetRef(loadingUi, "m_ProgressText", progText.GetComponent<TMP_Text>());
            loading.SetActive(false);

            SetRef(controller, "m_Loading", loadingUi);
            // The water overlay itself is built by step 5, which also assigns m_Water.
        }

        // ───────────────────────────── Helpers ─────────────────────────────

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject NewText(string name, string text, Transform parent, float size, FontStyles style)
        {
            var go = NewUI(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }

        private static Transform FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
                if (go.name == name) return go.transform;
            return null;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static void SetRef(Component comp, string field, Object value)
        {
            if (comp == null) { Debug.LogError($"[Wiring] Null component for field '{field}'."); return; }
            if (value == null) Debug.LogWarning($"[Wiring] {comp.GetType().Name}.{field} set to null (missing target).");
            var so = new SerializedObject(comp);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Wiring] {comp.GetType().Name} has no serialized field '{field}'."); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
