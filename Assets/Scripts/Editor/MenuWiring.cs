using System.IO;
using Loadout;
using TMPro;
using UI.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// One-shot wiring for the new menu flow: adds the runtime components onto the card/selector prefabs
    /// and onto the Menu scene's [Screen1/2/3] + SettingsPopup + MainBG, and assigns every serialized
    /// reference by hierarchy name. Re-runnable. Driven via the live Editor; see the PoolPatrol/Menu menu.
    /// </summary>
    public static class MenuWiring
    {
        private const string ArenaCardPath  = "Assets/Prefabs/UI/Menu/ArenaScreen/ArenaCard.prefab";
        private const string LevelCardPath  = "Assets/Prefabs/UI/Menu/LevelScreen/LevelCard.prefab";
        private const string LoadoutItemPath = "Assets/Prefabs/UI/Menu/LoadoutScreen/LoadoutSelectionItem.prefab";
        private const string BlurMatPath    = "Assets/Materials/UIBlur.mat";

        // ───────────────────────────── Prefabs ─────────────────────────────

        [MenuItem("PoolPatrol/Menu/1. Wire Card & Item Prefabs")]
        public static void WirePrefabs()
        {
            WirePrefab(ArenaCardPath, root =>
            {
                var c = GetOrAdd<ArenaCard>(root);
                SetRef(c, "m_NameText", Child(root, "name_&_desc/arena_name_text").GetComponent<TMP_Text>());
                SetRef(c, "m_DescText", Child(root, "name_&_desc/arena_desc_text").GetComponent<TMP_Text>());
                SetRef(c, "m_Icon", Child(root, "arena_icon").GetComponent<Image>());
                SetRef(c, "m_Highlight", Child(root, "highlight").gameObject);
                SetRef(c, "m_Locked", Child(root, "Locked").gameObject);
                SetRef(c, "m_Button", root.GetComponent<Button>());
            });

            WirePrefab(LevelCardPath, root =>
            {
                var c = GetOrAdd<LevelCard>(root);
                SetRef(c, "m_NumberText", Child(root, "level_index_text").GetComponent<TMP_Text>());
                SetRef(c, "m_Highlight", Child(root, "highlight").gameObject);
                SetRef(c, "m_Locked", Child(root, "Locked").gameObject);
                SetRef(c, "m_Button", root.GetComponent<Button>());
            });

            WirePrefab(LoadoutItemPath, root =>
            {
                var c = GetOrAdd<LoadoutSelectionItem>(root);
                var button = GetOrAdd<Button>(root);
                SetRef(c, "m_Icon", Child(root, "icon").GetComponent<Image>());
                SetRef(c, "m_Highlight", Child(root, "highlight").gameObject);
                SetRef(c, "m_Button", button);
            });

            AssetDatabase.SaveAssets();
            Debug.Log("[MenuWiring] Card & item prefabs wired.");
        }

        // ───────────────────────────── Menu scene ─────────────────────────────

        [MenuItem("PoolPatrol/Menu/2. Wire Menu Scene")]
        public static void WireMenuScene()
        {
            EnsureBlurMaterial();

            Transform canvas = FindRoot("Canvas");
            if (canvas == null) { Debug.LogError("[MenuWiring] Open Menu.unity (no Canvas found)."); return; }

            // MainBG: blur material + controller.
            var mainBg = canvas.Find("MainBG").GetComponent<Image>();
            mainBg.material = AssetDatabase.LoadAssetAtPath<Material>(BlurMatPath);
            GetOrAdd<MenuBackgroundBlur>(mainBg.gameObject);

            Transform flow = canvas.Find("GameFlowUI");
            RemoveComponent<global::UI.Menu.MenuFlowController>(flow);

            Transform s1 = flow.Find("[Screen1] ArenaSelection");
            Transform s2 = flow.Find("[Screen2] LevelSelection");
            Transform s3 = flow.Find("[Screen3] LoadoutSelection");
            Transform settings = flow.Find("SettingsPopup");

            // ── Arena screen + carousel ──────────────────────────────────
            Transform arenas = s1.Find("Arenas");
            var arenasImg = GetOrAdd<Image>(arenas.gameObject);
            arenasImg.color = new Color(0, 0, 0, 0);   // transparent raycast target so the whole area is draggable
            arenasImg.raycastTarget = true;

            var carousel = GetOrAdd<ArenaCarousel>(arenas.gameObject);
            SetRef(carousel, "m_CardPrefab", LoadComponent<ArenaCard>(ArenaCardPath));
            SetRef(carousel, "m_Container", arenas.Find("Container"));
            SetRef(carousel, "m_LeftButton", arenas.Find("Btn_Left").GetComponent<Button>());
            SetRef(carousel, "m_RightButton", arenas.Find("Btn_Right").GetComponent<Button>());

            var arenaScreen = GetOrAdd<ArenaSelectionScreen>(s1.gameObject);
            SetRef(arenaScreen, "m_Carousel", carousel);
            SetRef(arenaScreen, "m_NextButton", s1.Find("Btn_Play").GetComponent<Button>());

            // ── Level screen ─────────────────────────────────────────────
            var levelScreen = GetOrAdd<LevelSelectionScreen>(s2.gameObject);
            SetRef(levelScreen, "m_ArenaTitleText", s2.Find("Container/Title/ArenaName_text").GetComponent<TMP_Text>());
            SetRef(levelScreen, "m_CardContainer", s2.Find("Container/Body_ScrollView/Viewport/Content"));
            SetRef(levelScreen, "m_CardPrefab", LoadComponent<LevelCard>(LevelCardPath));
            SetRef(levelScreen, "m_NextButton", s2.Find("Btn_Next").GetComponent<Button>());
            SetRef(levelScreen, "m_BackButton", s2.Find("Container/Title/Btn_Back").GetComponent<Button>());

            // ── Loadout screen ───────────────────────────────────────────
            WireLoadoutScreen(s3);

            // ── Settings popup (component lives on always-active GameFlowUI) ─
            WireSettings(flow, settings, s1);

            // ── Navigator ────────────────────────────────────────────────
            var nav = GetOrAdd<MenuNavigator>(flow.gameObject);
            SetRef(nav, "m_ArenaScreen", arenaScreen);
            SetRef(nav, "m_LevelScreen", levelScreen);
            SetRef(nav, "m_LoadoutScreen", s3.GetComponent<LoadoutScreen>());
            SetRef(nav, "m_Blur", mainBg.GetComponent<MenuBackgroundBlur>());
            SetRef(nav, "m_PlayButtonAnchor", s3.Find("Btn_Play"));

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
            Debug.Log("[MenuWiring] Menu scene wired and saved.");
        }

        private static void WireLoadoutScreen(Transform s3)
        {
            var loadout = GetOrAdd<LoadoutScreen>(s3.gameObject);

            var character = WireSelector(s3.Find("selection_panel/character_selection"));
            var floatie   = WireSelector(s3.Find("selection_panel/floatie_selection"));
            var weapon    = WireSelector(s3.Find("selection_panel/weapon_selection"));
            var pool      = WireSelector(s3.Find("selection_panel/pool_texture_selection"));

            SetRef(loadout, "m_CharacterSelector", character);
            SetRef(loadout, "m_FloatieSelector", floatie);
            SetRef(loadout, "m_WeaponSelector", weapon);
            SetRef(loadout, "m_PoolTextureSelector", pool);

            var avatar = BuildAvatar(s3.Find("avatar_placeholder"));
            SetRef(loadout, "m_Avatar", avatar);

            Transform desc = s3.Find("selected_weapon_description");
            SetRef(loadout, "m_WeaponNameText", desc.Find("weapon_name_text").GetComponent<TMP_Text>());
            SetRef(loadout, "m_WeaponIcon", desc.Find("icon").GetComponent<Image>());
            SetRef(loadout, "m_WeaponDescriptionText", desc.Find("container/description_text/Text (TMP)").GetComponent<TMP_Text>());
            SetRef(loadout, "m_ComboTitleText", desc.Find("container/combo_info_text/combo_title").GetComponent<TMP_Text>());
            SetRef(loadout, "m_ComboInfoText", desc.Find("container/combo_info_text/description_text").GetComponent<TMP_Text>());

            SetRef(loadout, "m_PlayButton", s3.Find("Btn_Play").GetComponent<Button>());
            SetRef(loadout, "m_BackButton", s3.Find("TopLeft/Btn_Back").GetComponent<Button>());
        }

        private static LoadoutSelectorUI WireSelector(Transform selection)
        {
            var sel = GetOrAdd<LoadoutSelectorUI>(selection.gameObject);
            Transform container = selection.Find("container");
            Transform scroll = container.Find("Scroll View");

            var left = GetOrAdd<Button>(container.Find("icon_left").gameObject);
            var right = GetOrAdd<Button>(container.Find("icon_right").gameObject);

            SetRef(sel, "m_LeftButton", left);
            SetRef(sel, "m_RightButton", right);
            SetRef(sel, "m_Content", scroll.Find("Viewport/Content"));
            SetRef(sel, "m_ItemPrefab", LoadComponent<LoadoutSelectionItem>(LoadoutItemPath));
            SetRef(sel, "m_ScrollRect", scroll.GetComponent<ScrollRect>());
            return sel;
        }

        private static LoadoutAvatarPreview BuildAvatar(Transform placeholder)
        {
            var avatar = GetOrAdd<LoadoutAvatarPreview>(placeholder.gameObject);

            Transform animateRoot = EnsureChild(placeholder, "AnimateRoot");
            Image floatie = EnsureChildImage(animateRoot, "Floatie");
            Image head = EnsureChildImage(animateRoot, "Head");
            Image weapon = EnsureChildImage(animateRoot, "Weapon");
            Image ripple = EnsureChildImage(placeholder, "RippleTemplate");

            // Ripple sits behind the avatar and uses a soft built-in sprite as a placeholder ring.
            ripple.transform.SetAsFirstSibling();
            ripple.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            ripple.color = new Color(1f, 1f, 1f, 0.3f);
            ripple.raycastTarget = false;
            ripple.gameObject.SetActive(false);

            foreach (Image img in new[] { floatie, head, weapon })
            {
                img.raycastTarget = false;
                img.preserveAspect = true;
                img.enabled = false;   // enabled when a sprite is assigned at runtime
            }

            SetRef(avatar, "m_AnimateRoot", animateRoot);
            SetRef(avatar, "m_FloatieImage", floatie);
            SetRef(avatar, "m_HeadImage", head);
            SetRef(avatar, "m_WeaponImage", weapon);
            SetRef(avatar, "m_RippleTemplate", ripple);
            SetRef(avatar, "m_RippleParent", placeholder);
            return avatar;
        }

        private static void WireSettings(Transform flow, Transform settings, Transform s1)
        {
            var panel = GetOrAdd<SettingsPanelUI>(flow.gameObject);
            SetRef(panel, "m_Root", settings.gameObject);
            SetRef(panel, "m_Box", settings.Find("Container"));
            SetRef(panel, "m_OpenButton", s1.Find("Btn_Settings").GetComponent<Button>());
            SetRef(panel, "m_CloseButton", settings.Find("Btn_Close").GetComponent<Button>());

            Transform body = settings.Find("Container/Body");
            SetRef(panel, "m_MusicSlider", body.Find("Music/Slider").GetComponent<Slider>());
            SetRef(panel, "m_SfxSlider", body.Find("SFX/Slider").GetComponent<Slider>());
            SetRef(panel, "m_HapticsToggle", body.Find("Haptics/Toggle").GetComponent<Toggle>());
            SetRef(panel, "m_MusicValueText", body.Find("Music/SliderValueText").GetComponent<TMP_Text>());
            SetRef(panel, "m_SfxValueText", body.Find("SFX/SliderValueText").GetComponent<TMP_Text>());

            Transform footer = settings.Find("Container/Footer");
            SetRef(panel, "m_TermsButton", footer.Find("TOS").GetComponent<Button>());
            SetRef(panel, "m_PrivacyButton", footer.Find("PP").GetComponent<Button>());
        }

        private static void EnsureBlurMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(BlurMatPath) != null) return;
            Shader shader = Shader.Find("PoolPatrol/UIBlur");
            if (shader == null) { Debug.LogError("[MenuWiring] PoolPatrol/UIBlur shader not found."); return; }
            var mat = new Material(shader);
            mat.SetFloat("_BlurSize", 4f);
            AssetDatabase.CreateAsset(mat, BlurMatPath);
        }

        // ───────────────────────────── Helpers ─────────────────────────────

        private static void WirePrefab(string path, System.Action<GameObject> wire)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try { wire(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static T LoadComponent<T>(string prefabPath) where T : Component
            => AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<T>();

        private static Transform FindRoot(string name)
        {
            foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == name) return go.transform;
            return null;
        }

        private static Transform Child(GameObject root, string path)
        {
            Transform t = root.transform.Find(path);
            if (t == null) Debug.LogError($"[MenuWiring] Missing child '{path}' under {root.name}");
            return t;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static void RemoveComponent<T>(Transform t) where T : Component
        {
            T c = t.GetComponent<T>();
            if (c != null) Object.DestroyImmediate(c);
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;

            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Image EnsureChildImage(Transform parent, string name)
        {
            Transform t = EnsureChild(parent, name);
            return GetOrAdd<Image>(t.gameObject);
        }

        private static void SetRef(Component comp, string field, Object value)
        {
            if (value == null) Debug.LogWarning($"[MenuWiring] {comp.GetType().Name}.{field} set to null (missing target).");
            var so = new SerializedObject(comp);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[MenuWiring] {comp.GetType().Name} has no serialized field '{field}'."); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
