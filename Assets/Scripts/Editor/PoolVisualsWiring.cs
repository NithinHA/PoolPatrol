using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PoolPatrol.Editor
{
    /// <summary>
    /// Builds the fake-3D pool look in the Game scene: bakes the procedural textures, creates the
    /// water materials, swaps the flat tile sprites for the PoolFloor / PoolSurface / PoolDeck layers,
    /// and gives every floating prefab a <see cref="WaterContactFX"/>.
    /// Every step is re-runnable: it finds-or-creates by name and overwrites its own settings.
    /// </summary>
    public static class PoolVisualsWiring
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string Root = "Assets/Materials/Water";
        private const string TexturesDir = Root + "/Textures";
        private const string CausticsPath = TexturesDir + "/Caustics.png";
        private const string SoftCirclePath = TexturesDir + "/SoftCircle.png";
        private const string FloorMatPath = Root + "/PoolFloor.mat";
        private const string SurfaceMatPath = Root + "/PoolSurface.mat";
        private const string DeckMatPath = Root + "/PoolDeck.mat";
        private const string ShadowMatPath = Root + "/UnderwaterShadow.mat";
        private const string RingMatPath = Root + "/WaterlineRing.mat";
        private const string SourceProfilePath = "Assets/Settings/SampleSceneProfile.asset";
        private const string GameplayProfilePath = "Assets/Settings/PoolGameplayProfile.asset";

        private const string WaterSortingLayer = "Water";
        private const string ForegroundSortingLayer = "Foreground";
        private const int SurfaceOrder = 10;
        private const int DeckOrder = 30;

        private static readonly string[] DuckPrefabs =
        {
            "Assets/Prefabs/Enemies/RubberDuck_chase.prefab",
            "Assets/Prefabs/Enemies/RubberDuck_gemDropper.prefab",
            "Assets/Prefabs/Enemies/RubberDuck_heartDropper.prefab",
            "Assets/Prefabs/Enemies/RubberDuck_idle.prefab",
            "Assets/Prefabs/Enemies/RubberDuck_randomMove.prefab",
            "Assets/Prefabs/Enemies/RubberDuck_shooter.prefab",
        };
        private static readonly string[] PickupPrefabs =
        {
            "Assets/Prefabs/DroppedItems/gem_blue.prefab",
            "Assets/Prefabs/DroppedItems/heart.prefab",
        };
        private const string FloatiePrefab = "Assets/Prefabs/Player variants/Floaties/DefaultFloatie.prefab";

        [MenuItem("PoolPatrol/Pool Visuals/Run All Steps")]
        public static void RunAll()
        {
            BakeTextures();
            CreateMaterials();
            WireGameScene();
            WirePrefabs();
        }

#region 1. Textures

        [MenuItem("PoolPatrol/Pool Visuals/1. Bake Water Textures")]
        public static void BakeTextures()
        {
            Directory.CreateDirectory(TexturesDir);

            File.WriteAllBytes(CausticsPath, BakeCaustics(512).EncodeToPNG());
            File.WriteAllBytes(SoftCirclePath, BakeSoftCircle(128).EncodeToPNG());
            AssetDatabase.ImportAsset(CausticsPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(SoftCirclePath, ImportAssetOptions.ForceSynchronousImport);

            var caustics = (TextureImporter)AssetImporter.GetAtPath(CausticsPath);
            caustics.textureType = TextureImporterType.SingleChannel;
            caustics.sRGBTexture = false;
            caustics.wrapMode = TextureWrapMode.Repeat;
            caustics.mipmapEnabled = true;
            var causticsSettings = new TextureImporterSettings();
            caustics.ReadTextureSettings(causticsSettings);
            causticsSettings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;   // shaders read .r
            caustics.SetTextureSettings(causticsSettings);
            caustics.SaveAndReimport();

            var circle = (TextureImporter)AssetImporter.GetAtPath(SoftCirclePath);
            circle.textureType = TextureImporterType.Sprite;
            circle.spriteImportMode = SpriteImportMode.Single;
            circle.spritePixelsPerUnit = 128;
            circle.wrapMode = TextureWrapMode.Clamp;
            circle.alphaIsTransparency = true;
            circle.mipmapEnabled = false;
            var settings = new TextureImporterSettings();
            circle.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;   // the ring shader needs a whole quad
            circle.SetTextureSettings(settings);
            circle.SaveAndReimport();

            Debug.Log("[PoolVisualsWiring] Baked Caustics.png and SoftCircle.png.");
        }

        /// <summary>
        /// Tileable caustic web: the cell borders of a periodic Voronoi (F2 - F1), domain-warped with
        /// whole-period sines so the lines curve but still wrap seamlessly. Two octaves.
        /// </summary>
        private static Texture2D BakeCaustics(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float wu = u + 0.025f * Mathf.Sin(2f * Mathf.PI * (2f * v + 0.13f)) + 0.015f * Mathf.Sin(2f * Mathf.PI * (3f * u + v + 0.71f));
                float wv = v + 0.025f * Mathf.Sin(2f * Mathf.PI * (2f * u + 0.37f)) + 0.015f * Mathf.Sin(2f * Mathf.PI * (3f * v - u + 0.29f));

                float a = Web(wu, wv, 5, 0.17f, 11);
                float b = Web(wu + 0.31f, wv + 0.17f, 9, 0.12f, 29);
                float c = Mathf.Clamp01(a + b * 0.45f);
                pixels[y * size + x] = new Color(c, c, c, 1f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static float Web(float u, float v, int cells, float width, int seed)
        {
            float px = Frac(u) * cells, py = Frac(v) * cells;
            int ix = Mathf.FloorToInt(px), iy = Mathf.FloorToInt(py);
            float f1 = 9f, f2 = 9f;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = ix + dx, cy = iy + dy;
                int wx = ((cx % cells) + cells) % cells, wy = ((cy % cells) + cells) % cells;
                float fx = cx + 0.15f + 0.7f * Hash(wx, wy, seed);
                float fy = cy + 0.15f + 0.7f * Hash(wy, wx, seed + 7);
                float d = Mathf.Sqrt((fx - px) * (fx - px) + (fy - py) * (fy - py));
                if (d < f1) { f2 = f1; f1 = d; }
                else if (d < f2) f2 = d;
            }
            float line = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((f2 - f1) / width));
            return line * line;
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) / (float)uint.MaxValue;
            }
        }

        private static float Frac(float f) => f - Mathf.Floor(f);

        private static Texture2D BakeSoftCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = 1f - Mathf.SmoothStep(0.25f, 1f, r);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

#endregion

#region 2. Materials

        [MenuItem("PoolPatrol/Pool Visuals/2. Create Water Materials")]
        public static void CreateMaterials()
        {
            var caustics = AssetDatabase.LoadAssetAtPath<Texture2D>(CausticsPath);
            if (caustics == null)
            {
                Debug.LogError("[PoolVisualsWiring] Caustics texture missing — run step 1 first.");
                return;
            }

            EnsureMaterial(FloorMatPath, "PoolPatrol/PoolFloor")?.SetTexture("_CausticsTex", caustics);
            EnsureMaterial(SurfaceMatPath, "PoolPatrol/PoolSurface")?.SetTexture("_CausticsTex", caustics);
            EnsureMaterial(DeckMatPath, "PoolPatrol/PoolDeck");
            EnsureMaterial(ShadowMatPath, "PoolPatrol/UnderwaterShadow");
            EnsureMaterial(RingMatPath, "PoolPatrol/WaterlineRing");
            AssetDatabase.SaveAssets();
            Debug.Log("[PoolVisualsWiring] Water materials ready in " + Root);
        }

        /// <summary>Creates the material if missing; an existing one keeps its tuned values.</summary>
        private static Material EnsureMaterial(string path, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[PoolVisualsWiring] Shader '{shaderName}' not found — check it compiled.");
                return null;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

#endregion

#region 3. Scene

        [MenuItem("PoolPatrol/Pool Visuals/3. Wire Game Scene")]
        public static void WireGameScene()
        {
            EnsureSortingLayer(WaterSortingLayer, "Default", before: true);
            EnsureSortingLayer(ForegroundSortingLayer, "Player", before: false);

            var floorMat = AssetDatabase.LoadAssetAtPath<Material>(FloorMatPath);
            var surfaceMat = AssetDatabase.LoadAssetAtPath<Material>(SurfaceMatPath);
            var deckMat = AssetDatabase.LoadAssetAtPath<Material>(DeckMatPath);
            if (floorMat == null || surfaceMat == null || deckMat == null)
            {
                Debug.LogError("[PoolVisualsWiring] Water materials missing — run step 2 first.");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != GameScenePath)
                scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            Transform bg = FindRoot(scene, "Bg");
            Transform edges = bg != null ? bg.Find("Edge Colliders") : null;
            Transform water = bg != null ? bg.Find("Background") : null;
            if (edges == null || water == null)
            {
                Debug.LogError("[PoolVisualsWiring] Expected Bg/Edge Colliders and Bg/Background in Game.unity.");
                return;
            }

            // Pool shape + sun.
            Transform envTf = FindOrCreateChild(bg, "PoolEnvironment");
            var env = envTf.GetComponent<PoolEnvironment>();
            if (env == null) env = envTf.gameObject.AddComponent<PoolEnvironment>();
            var envSo = new SerializedObject(env);
            envSo.FindProperty("m_LeftWall").objectReferenceValue = edges.Find("cube_left").GetComponent<BoxCollider2D>();
            envSo.FindProperty("m_RightWall").objectReferenceValue = edges.Find("cube_right").GetComponent<BoxCollider2D>();
            envSo.FindProperty("m_TopWall").objectReferenceValue = edges.Find("cube_top").GetComponent<BoxCollider2D>();
            envSo.FindProperty("m_BottomWall").objectReferenceValue = edges.Find("cube_bottom").GetComponent<BoxCollider2D>();
            envSo.FindProperty("m_FloorMaterial").objectReferenceValue = floorMat;   // renders the floor bake
            envSo.ApplyModifiedPropertiesWithoutUndo();

            // The flat tile art is replaced by the shader layers; the colliders stay (walls + camera confiner).
            foreach (var sr in edges.GetComponentsInChildren<SpriteRenderer>(true))
                sr.enabled = false;

            Bounds arena = WorldBounds(edges.Find("full").GetComponent<BoxCollider2D>());

            // Floor: opaque, behind everything (Bg sits at z = 5).
            Transform floor = FindOrCreateQuad(bg, "PoolFloor");
            PlaceQuad(floor, arena.center, (Vector2)arena.size + new Vector2(2f, 2f), 6.5f);
            ConfigureRenderer(floor.GetComponent<MeshRenderer>(), floorMat, WaterSortingLayer, -100);

            // Surface: the existing ripple plane keeps its RippleEffect, just with the new material.
            var waterRenderer = water.GetComponent<MeshRenderer>();
            ConfigureRenderer(waterRenderer, surfaceMat, WaterSortingLayer, SurfaceOrder);
            var ripple = water.GetComponent<RippleEffect>();
            if (ripple != null)
            {
                Undo.RecordObject(ripple, "Wire ripple camera");
                ripple.RippleCamera = water.GetComponentInChildren<Camera>(true);
                EditorUtility.SetDirty(ripple);
            }

            // Deck: large quad with the pool cut out, above the water, below gameplay.
            Transform deck = FindOrCreateQuad(bg, "PoolDeck");
            PlaceQuad(deck, arena.center, (Vector2)arena.size + new Vector2(24f, 24f), 4.5f);
            ConfigureRenderer(deck.GetComponent<MeshRenderer>(), deckMat, WaterSortingLayer, DeckOrder);

            // Lily pads float too.
            foreach (Transform child in bg)
                if (child.name.StartsWith("LiliPads"))
                    AddContactFX(child.gameObject, child.GetComponent<SpriteRenderer>(), 0.8f, ringIntensity: 0.7f, shadowHeight: 0.4f);

            WireGameplayVolume(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PoolVisualsWiring] Game scene wired and saved.");
        }

        /// <summary>
        /// Gives the Game scene its own copy of the volume profile, with a soft bloom so foam and glints
        /// glow and a light vignette to pull the eye to the pool, leaving other scenes untouched.
        /// </summary>
        private static void WireGameplayVolume(UnityEngine.SceneManagement.Scene scene)
        {
            Transform volumeTf = FindRoot(scene, "Global Volume");
            var volume = volumeTf != null ? volumeTf.GetComponent<Volume>() : null;
            if (volume == null)
            {
                Debug.LogWarning("[PoolVisualsWiring] No Global Volume in Game.unity; skipping post-processing.");
                return;
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GameplayProfilePath);
            if (profile == null)
            {
                if (!AssetDatabase.CopyAsset(SourceProfilePath, GameplayProfilePath)) return;
                profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GameplayProfilePath);
            }

            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);

            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.45f);

            if (!profile.TryGet(out ColorAdjustments color)) color = profile.Add<ColorAdjustments>();
            color.active = true;
            color.saturation.Override(10f);
            color.contrast.Override(6f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            Undo.RecordObject(volume, "Assign gameplay profile");
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);
        }

#endregion

#region 4. Prefabs

        [MenuItem("PoolPatrol/Pool Visuals/4. Add Water FX To Prefabs")]
        public static void WirePrefabs()
        {
            foreach (string path in DuckPrefabs)
                EditPrefab(path, root => AddContactFX(root, FindVisibleSprite(root, "Body"), 0.85f, 1f, 1f));

            foreach (string path in PickupPrefabs)
                EditPrefab(path, root => AddContactFX(root, FindVisibleSprite(root, "gfx"), 0.75f, 0.55f, 1.5f));

            EditPrefab(FloatiePrefab, root =>
            {
                Transform gfx = root.transform.Find("GfxParent/Gfx");
                AddContactFX(root, gfx != null ? gfx.GetComponent<SpriteRenderer>() : null, 0.95f, 1f, 1f);
                // The old fake reflection fights the new underwater shadow.
                Transform reflection = root.transform.Find("GfxParent/Reflection");
                if (reflection != null) reflection.gameObject.SetActive(false);
            });

            AssetDatabase.SaveAssets();
            Debug.Log("[PoolVisualsWiring] WaterContactFX added to duck, pickup and floatie prefabs.");
        }

        private static void EditPrefab(string path, System.Action<GameObject> edit)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[PoolVisualsWiring] Prefab not found: {path}");
                return;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static SpriteRenderer FindVisibleSprite(GameObject root, string preferredName)
        {
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.name == preferredName && sr.enabled && sr.sprite != null) return sr;

            SpriteRenderer best = null;
            float bestWidth = 0f;
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!sr.enabled || !sr.gameObject.activeSelf || sr.sprite == null) continue;
                float w = Mathf.Abs(sr.sprite.bounds.size.x * sr.transform.lossyScale.x);
                if (w > bestWidth) { best = sr; bestWidth = w; }
            }
            return best;
        }

        /// <param name="ringFit">Waterline diameter as a fraction of the sprite's width.</param>
        private static void AddContactFX(GameObject target, SpriteRenderer source, float ringFit, float ringIntensity, float shadowHeight)
        {
            if (source == null)
            {
                Debug.LogWarning($"[PoolVisualsWiring] No visible sprite on {target.name}; skipped.");
                return;
            }

            var fx = target.GetComponent<WaterContactFX>();
            if (fx == null) fx = target.AddComponent<WaterContactFX>();
            var so = new SerializedObject(fx);
            so.FindProperty("m_ShadowSource").objectReferenceValue = source;
            so.FindProperty("m_BlobSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SoftCirclePath);
            so.FindProperty("m_ShadowMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(ShadowMatPath);
            so.FindProperty("m_ShadowHeight").floatValue = shadowHeight;
            so.FindProperty("m_RingSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SoftCirclePath);
            so.FindProperty("m_RingMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(RingMatPath);
            so.FindProperty("m_RingDiameter").floatValue = Mathf.Abs(source.sprite.bounds.size.x * source.transform.lossyScale.x) * ringFit;
            so.FindProperty("m_RingIntensity").floatValue = ringIntensity;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

#endregion

#region Helpers

        private static void EnsureSortingLayer(string layerName, string neighbour, bool before)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");
            int neighbourIndex = -1;
            for (int i = 0; i < layers.arraySize; i++)
            {
                string n = layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                if (n == layerName) return;
                if (n == neighbour) neighbourIndex = i;
            }

            int index = neighbourIndex < 0 ? layers.arraySize : (before ? neighbourIndex : neighbourIndex + 1);
            layers.InsertArrayElementAtIndex(index);
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            layer.FindPropertyRelative("name").stringValue = layerName;
            layer.FindPropertyRelative("uniqueID").uintValue = (uint)Random.Range(1, int.MaxValue);
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[PoolVisualsWiring] Added sorting layer '{layerName}'.");
        }

        private static Transform FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
                if (go.name == name) return go.transform;
            return null;
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform FindOrCreateQuad(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void PlaceQuad(Transform quad, Vector3 center, Vector2 size, float worldZ)
        {
            quad.SetPositionAndRotation(new Vector3(center.x, center.y, worldZ), Quaternion.identity);
            Vector3 parentScale = quad.parent != null ? quad.parent.lossyScale : Vector3.one;
            quad.localScale = new Vector3(size.x / parentScale.x, size.y / parentScale.y, 1f);
        }

        private static void ConfigureRenderer(Renderer renderer, Material material, string sortingLayer, int order)
        {
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            EditorUtility.SetDirty(renderer);
        }

        private static Bounds WorldBounds(BoxCollider2D box)
        {
            Transform t = box.transform;
            Vector2 h = box.size * 0.5f;
            var b = new Bounds(t.TransformPoint(box.offset), Vector3.zero);
            b.Encapsulate(t.TransformPoint(box.offset - h));
            b.Encapsulate(t.TransformPoint(box.offset + h));
            return b;
        }

#endregion
    }
}
