using UI.Transition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// Builds the shader-driven water transition under the Bootstrap scene's persistent TransitionCanvas
    /// and points <see cref="SceneTransitionController"/> at it, replacing any earlier transition.
    /// Re-runnable: it rebuilds the overlay from scratch each time.
    /// </summary>
    public static class WaterFloodWiring
    {
        private const string BootScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string MaterialPath = "Assets/Materials/WaterFlood.mat";
        private const string ShaderName = "PoolPatrol/WaterFlood";

        [MenuItem("PoolPatrol/Menu/5. Wire Water Screen Transition")]
        public static void Wire()
        {
            Material material = EnsureMaterial();
            if (material == null) return;

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);

            Transform canvas = FindRoot(scene, "TransitionCanvas");
            if (canvas == null)
            {
                Debug.LogError("[WaterFloodWiring] No TransitionCanvas in Bootstrap.unity — run step 4 first.");
                return;
            }

            // Retire every earlier transition build.
            DestroyChild(canvas, "Water");
            DestroyChild(canvas, "WaveTransition");
            DestroyChild(canvas, "WaterFlood");

            GameObject root = NewFullScreen("WaterFlood", canvas);
            var surface = root.AddComponent<RawImage>();
            surface.material = material;
            surface.raycastTarget = true;   // blocks input for the duration of the transition
            var effect = root.AddComponent<WaterFloodTransition>();

            var so = new SerializedObject(effect);
            so.FindProperty("m_Root").objectReferenceValue = root;
            so.FindProperty("m_Surface").objectReferenceValue = surface;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Draw above the loading panel, and start hidden.
            root.transform.SetAsLastSibling();
            root.SetActive(false);

            var controller = canvas.GetComponent<SceneTransitionController>();
            if (controller != null)
            {
                var cso = new SerializedObject(controller);
                cso.FindProperty("m_Water").objectReferenceValue = effect;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogError("[WaterFloodWiring] SceneTransitionController not found on TransitionCanvas.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[WaterFloodWiring] Water transition wired. Bootstrap scene saved.");
        }

        /// <summary>Creates the material if missing. The shader needs no textures.</summary>
        private static Material EnsureMaterial()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[WaterFloodWiring] Shader '{ShaderName}' not found — check it compiled.");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static GameObject NewFullScreen(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static Transform FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
                if (go.name == name) return go.transform;
            return null;
        }
    }
}
