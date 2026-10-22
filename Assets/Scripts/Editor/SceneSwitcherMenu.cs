using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// Adds a top-level "Scene" menu for jumping between the project's main scenes. Prompts to save
    /// unsaved changes first, and is disabled while in Play mode.
    /// </summary>
    public static class SceneSwitcherMenu
    {
        private const string BootstrapPath = "Assets/Scenes/Bootstrap.unity";
        private const string MenuPath      = "Assets/Scenes/Menu.unity";
        private const string GamePath      = "Assets/Scenes/Game.unity";

        [MenuItem("Scene/Bootstrap", priority = 0)]
        private static void OpenBootstrap() => Open(BootstrapPath);

        [MenuItem("Scene/Menu", priority = 1)]
        private static void OpenMenu() => Open(MenuPath);

        [MenuItem("Scene/Game", priority = 2)]
        private static void OpenGame() => Open(GamePath);

        [MenuItem("Scene/Bootstrap", true)]
        [MenuItem("Scene/Menu", true)]
        [MenuItem("Scene/Game", true)]
        private static bool CanSwitch() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void Open(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogError($"[SceneSwitcherMenu] No scene at '{path}'.");
                return;
            }

            // Returns false if the user hits Cancel on the save prompt.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
