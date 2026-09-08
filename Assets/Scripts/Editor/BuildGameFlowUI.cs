using TMPro;
using UI.Menu;
using UI.PostGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// Builds dummy (grey-box) UI for the Arena Selection -> Level Selection -> Game -> Post-Game
    /// loop, wired up to the runtime scripts under Assets/Scripts/UI. Meant as a working starting
    /// point to re-skin, not final art.
    ///
    /// Run "Build Menu Flow UI" while Menu.unity is open, and "Build PostGame UI" while Game.unity
    /// is open. Both add a new child under the scene's existing Canvas — nothing pre-existing is
    /// removed.
    /// </summary>
    public static class BuildGameFlowUI
    {
        private const string PrefabFolder = "Assets/Prefabs/UI";

        [MenuItem("PoolPatrol/UI/Build Menu Flow UI")]
        public static void BuildMenuFlow()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[BuildGameFlowUI] No Canvas found in the active scene. Open Menu.unity first.");
                return;
            }

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                Debug.LogWarning("[BuildGameFlowUI] No EventSystem found in scene — UI clicks won't register until one is added.");

            EnsureFolder(PrefabFolder);
            var cardPrefab = BuildArenaCardPrefab();
            var levelButtonPrefab = BuildLevelButtonPrefab();

            var root = NewUI("GameFlowUI", canvas.transform);
            StretchFill(root.GetComponent<RectTransform>());

            var flow = root.AddComponent<MenuFlowController>();

            // ── Main Menu ────────────────────────────────────────────────
            var mainMenuPanel = BuildPanel(root.transform, "MainMenuPanel", new Color(0.05f, 0.1f, 0.2f, 1f));
            AddText(mainMenuPanel.transform, "Title", "Pool Patrol", 64, new Vector2(0f, 0.7f), new Vector2(1f, 0.95f));
            var playButton = AddButton(mainMenuPanel.transform, "PlayButton", "Play", new Vector2(0.35f, 0.4f), new Vector2(0.65f, 0.52f));

            // ── Arena Selection ──────────────────────────────────────────
            var arenaPanel = BuildPanel(root.transform, "ArenaSelectionPanel", new Color(0.08f, 0.15f, 0.25f, 1f));
            arenaPanel.SetActive(false);
            AddText(arenaPanel.transform, "Title", "Select Arena", 48, new Vector2(0f, 0.88f), new Vector2(1f, 0.98f));
            var arenaBackButton = AddButton(arenaPanel.transform, "BackButton", "Back", new Vector2(0.02f, 0.9f), new Vector2(0.18f, 0.98f));
            var cardContainerGO = NewUI("CardContainer", arenaPanel.transform);
            var cardContainerRT = cardContainerGO.GetComponent<RectTransform>();
            cardContainerRT.anchorMin = new Vector2(0.1f, 0.08f);
            cardContainerRT.anchorMax = new Vector2(0.9f, 0.85f);
            cardContainerRT.offsetMin = Vector2.zero;
            cardContainerRT.offsetMax = Vector2.zero;
            var arenaLayout = cardContainerGO.AddComponent<VerticalLayoutGroup>();
            arenaLayout.spacing = 12f;
            arenaLayout.childForceExpandHeight = false;
            arenaLayout.childControlHeight = false;
            var arenaUI = arenaPanel.AddComponent<ArenaSelectionUI>();

            // ── Level Selection (popup) ──────────────────────────────────
            var levelPopupRoot = BuildPanel(root.transform, "LevelSelectionPanel", new Color(0f, 0f, 0f, 0.6f));
            levelPopupRoot.SetActive(false);
            var popupBox = BuildPanel(levelPopupRoot.transform, "PopupBox", new Color(0.12f, 0.2f, 0.3f, 1f));
            var popupBoxRT = popupBox.GetComponent<RectTransform>();
            popupBoxRT.anchorMin = new Vector2(0.2f, 0.15f);
            popupBoxRT.anchorMax = new Vector2(0.8f, 0.85f);
            popupBoxRT.offsetMin = Vector2.zero;
            popupBoxRT.offsetMax = Vector2.zero;
            var arenaTitleText = AddText(popupBox.transform, "ArenaTitleText", "Arena Name", 40, new Vector2(0f, 0.85f), new Vector2(1f, 0.98f));
            var levelBackButton = AddButton(popupBox.transform, "BackButton", "Back", new Vector2(0.05f, 0.02f), new Vector2(0.3f, 0.12f));
            var levelButtonContainerGO = NewUI("LevelButtonContainer", popupBox.transform);
            var levelContainerRT = levelButtonContainerGO.GetComponent<RectTransform>();
            levelContainerRT.anchorMin = new Vector2(0.05f, 0.18f);
            levelContainerRT.anchorMax = new Vector2(0.95f, 0.8f);
            levelContainerRT.offsetMin = Vector2.zero;
            levelContainerRT.offsetMax = Vector2.zero;
            var gridLayout = levelButtonContainerGO.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120f, 120f);
            gridLayout.spacing = new Vector2(16f, 16f);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            var levelSelectionUI = levelPopupRoot.AddComponent<LevelSelectionUI>();

            // ── Wire references ─────────────────────────────────────────
            SetField(flow, "m_MainMenuPanel", mainMenuPanel);
            SetField(flow, "m_PlayButton", playButton);
            SetField(flow, "m_ArenaSelectionPanel", arenaPanel);
            SetField(flow, "m_ArenaSelectionUI", arenaUI);
            SetField(flow, "m_LevelSelectionPanel", levelPopupRoot);
            SetField(flow, "m_LevelSelectionUI", levelSelectionUI);

            SetField(arenaUI, "m_CardContainer", cardContainerGO.transform);
            SetField(arenaUI, "m_CardPrefab", cardPrefab);
            SetField(arenaUI, "m_BackButton", arenaBackButton);

            SetField(levelSelectionUI, "m_ArenaTitleText", arenaTitleText);
            SetField(levelSelectionUI, "m_LevelButtonContainer", levelButtonContainerGO.transform);
            SetField(levelSelectionUI, "m_LevelButtonPrefab", levelButtonPrefab);
            SetField(levelSelectionUI, "m_BackButton", levelBackButton);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("[BuildGameFlowUI] Menu flow UI built under 'GameFlowUI'. Assign ArenaDefinitionSO assets to Bootstrap's Arenas list before pressing Play.");
        }

        [MenuItem("PoolPatrol/UI/Build PostGame UI")]
        public static void BuildPostGame()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[BuildGameFlowUI] No Canvas found in the active scene. Open Game.unity first.");
                return;
            }

            var root = NewUI("PostGameUI", canvas.transform);
            StretchFill(root.GetComponent<RectTransform>());
            var postGame = root.AddComponent<PostGameUI>();

            var panel = BuildPanel(root.transform, "Panel", new Color(0f, 0f, 0f, 0.75f));
            panel.SetActive(false);
            var titleText = AddText(panel.transform, "TitleText", "Level Complete!", 56, new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f));
            var retryButton = AddButton(panel.transform, "RetryButton", "Retry", new Vector2(0.3f, 0.35f), new Vector2(0.5f, 0.47f));
            var mainMenuButton = AddButton(panel.transform, "MainMenuButton", "Main Menu", new Vector2(0.52f, 0.35f), new Vector2(0.72f, 0.47f));

            SetField(postGame, "m_Root", panel);
            SetField(postGame, "m_TitleText", titleText);
            SetField(postGame, "m_RetryButton", retryButton);
            SetField(postGame, "m_MainMenuButton", mainMenuButton);

            var levelManager = Object.FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
                SetField(levelManager, "m_PostGameUI", postGame);
            else
                Debug.LogWarning("[BuildGameFlowUI] No LevelManager found in scene — assign its Post Game UI field manually.");

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("[BuildGameFlowUI] Post-game UI built under 'PostGameUI'.");
        }

        #region Prefab building

        private static ArenaCardUI BuildArenaCardPrefab()
        {
            var go = new GameObject("ArenaCard", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(600f, 90f);

            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.15f);
            var button = go.AddComponent<Button>();

            var nameText = AddText(go.transform, "NameText", "Arena Name", 28, new Vector2(0.05f, 0.45f), new Vector2(0.75f, 0.95f), TextAlignmentOptions.Left);
            var statusText = AddText(go.transform, "StatusText", "0/5 Complete", 18, new Vector2(0.05f, 0.05f), new Vector2(0.75f, 0.45f), TextAlignmentOptions.Left);

            var lockIcon = AddText(go.transform, "LockIcon", "LOCKED", 24, new Vector2(0.76f, 0.15f), new Vector2(0.98f, 0.85f), TextAlignmentOptions.Right);
            lockIcon.color = Color.red;

            var card = go.AddComponent<ArenaCardUI>();
            SetField(card, "m_SelectButton", button);
            SetField(card, "m_NameText", nameText);
            SetField(card, "m_StatusText", statusText);
            SetField(card, "m_LockIcon", lockIcon.gameObject);

            var prefab = SaveAsPrefab(go, $"{PrefabFolder}/ArenaCard.prefab");
            return prefab.GetComponent<ArenaCardUI>();
        }

        private static LevelButtonUI BuildLevelButtonPrefab()
        {
            var go = new GameObject("LevelButton", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(120f, 120f);

            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.15f);
            var button = go.AddComponent<Button>();

            var levelNumberText = AddText(go.transform, "LevelNumberText", "1", 40, new Vector2(0f, 0f), new Vector2(1f, 1f));

            var lockIcon = AddText(go.transform, "LockIcon", "LOCK", 16, new Vector2(0f, 0f), new Vector2(1f, 0.3f));
            lockIcon.color = Color.red;

            var completedCheck = AddText(go.transform, "CompletedCheck", "DONE", 16, new Vector2(0f, 0.7f), new Vector2(1f, 1f));
            completedCheck.color = Color.green;

            var levelButton = go.AddComponent<LevelButtonUI>();
            SetField(levelButton, "m_SelectButton", button);
            SetField(levelButton, "m_LevelNumberText", levelNumberText);
            SetField(levelButton, "m_LockIcon", lockIcon.gameObject);
            SetField(levelButton, "m_CompletedCheck", completedCheck.gameObject);

            var prefab = SaveAsPrefab(go, $"{PrefabFolder}/LevelButton.prefab");
            return prefab.GetComponent<LevelButtonUI>();
        }

        private static GameObject SaveAsPrefab(GameObject source, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return prefab;
        }

        #endregion

        #region UI primitives

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static GameObject BuildPanel(Transform parent, string name, Color color)
        {
            var go = NewUI(name, parent);
            StretchFill(go.GetComponent<RectTransform>());
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private static TMP_Text AddText(Transform parent, string name, string content, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = NewUI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button AddButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = NewUI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var button = go.AddComponent<Button>();

            AddText(go.transform, "Label", label, 24, Vector2.zero, Vector2.one).color = Color.black;
            return button;
        }

        #endregion

        #region Field wiring

        private static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogError($"[BuildGameFlowUI] Field '{fieldName}' not found on {target.GetType().Name}.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        #endregion
    }
}
