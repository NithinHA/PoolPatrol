using UnityEditor;
using UnityEngine;

public class OptimizeDrawCalls
{
    [MenuItem("Tools/Optimize Draw Calls/Fix Particle Materials")]
    public static void FixParticleMaterials()
    {
        string[] searchPaths = new string[] { "Assets/Prefabs", "Assets/ThirdParty" };
        string[] guids = AssetDatabase.FindAssets("t:Prefab", searchPaths);
        
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/VFX"))
            AssetDatabase.CreateFolder("Assets/Materials", "VFX");
            
        Material sharedMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/SharedParticleURP.mat");
        if (sharedMat == null)
        {
            sharedMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            // Set base map to default particle texture (often handled gracefully if left null, rendering as a white square/circle)
            // But we actually want to retain whatever the previous material had if possible, though doing it generally is safer.
            // "Legacy Shaders/Particles/Alpha Blended Premultiply" usually has a _MainTex.
            AssetDatabase.CreateAsset(sharedMat, "Assets/Materials/VFX/SharedParticleURP.mat");
        }

        int modifiedCount = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool prefabModified = false;
            ParticleSystemRenderer[] renderers = prefab.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var renderer in renderers)
            {
                // Must clone array to assign it back
                Material[] sharedMats = renderer.sharedMaterials;
                bool rendererModified = false;
                
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    Material mat = sharedMats[i];
                    if (mat != null)
                    {
                        // Check if it's the specific GUID that was failing: e88ae70fdb7a9234bb51f64e1f7d21d0
                        string matPath = AssetDatabase.GetAssetPath(mat);
                        string matGuid = AssetDatabase.AssetPathToGUID(matPath);
                        
                        // Default-Material GUID is widely known as 'e88ae70fdb7a9234bb51f64e1f7d21d0', or legacy shader names
                        if (matGuid == "e88ae70fdb7a9234bb51f64e1f7d21d0" || 
                            (mat.shader != null && mat.shader.name == "Legacy Shaders/Particles/Alpha Blended Premultiply"))
                        {
                            sharedMats[i] = sharedMat;
                            rendererModified = true;
                        }
                    }
                }
                
                if (rendererModified)
                {
                    renderer.sharedMaterials = sharedMats;
                    prefabModified = true;
                }
                
                // Some Particles miss trailing materials that are in the renderer but not active, just ensure clean array
            }

            if (prefabModified)
            {
                EditorUtility.SetDirty(prefab);
                modifiedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"[OptimizeDrawCalls] Fixed materials on {modifiedCount} prefabs.");
    }
}
