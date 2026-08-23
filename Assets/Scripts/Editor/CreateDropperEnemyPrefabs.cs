using UnityEditor;
using UnityEngine;
using Enemy;
using Enemy.Death;

namespace PoolPatrol.Editor
{
    public static class CreateDropperEnemyPrefabs
    {
        private const string SourcePrefabPath  = "Assets/Prefabs/Enemies/RubberDuck_randomMove.prefab";
        private const string GemPrefabPath     = "Assets/Prefabs/Enemies/RubberDuck_gemDropper.prefab";
        private const string HeartPrefabPath   = "Assets/Prefabs/Enemies/RubberDuck_heartDropper.prefab";

        [MenuItem("PoolPatrol/Create Dropper Enemy Prefabs")]
        public static void CreatePrefabs()
        {
            CreateDropperPrefab<DropGemsOnDeath>(GemPrefabPath,   EnemyType.GemDropper);
            CreateDropperPrefab<DropHeartOnDeath>(HeartPrefabPath, EnemyType.HeartDropper);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PoolPatrol] Dropper enemy prefabs created.");
        }

        private static void CreateDropperPrefab<TDropEffect>(string outputPath, EnemyType enemyType)
            where TDropEffect : MonoBehaviour
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (source == null)
            {
                Debug.LogError($"[PoolPatrol] Source prefab not found at: {SourcePrefabPath}");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // Set EnemyType
            EnemyController controller = instance.GetComponent<EnemyController>();
            if (controller != null)
                controller.EnemyType = enemyType;
            else
                Debug.LogWarning($"[PoolPatrol] EnemyController not found on {SourcePrefabPath}");

            // Ensure DeathEffectHandler is present
            if (instance.GetComponent<EnemyDeathEffectHandler>() == null)
                instance.AddComponent<EnemyDeathEffectHandler>();

            // Ensure ThrowDeathParticlesOnDeath is present
            if (instance.GetComponent<ThrowDeathParticlesOnDeath>() == null)
                instance.AddComponent<ThrowDeathParticlesOnDeath>();

            // Add the drop effect if not already present
            if (instance.GetComponent<TDropEffect>() == null)
                instance.AddComponent<TDropEffect>();

            PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
            Object.DestroyImmediate(instance);

            Debug.Log($"[PoolPatrol] Created {outputPath}");
        }
    }
}
