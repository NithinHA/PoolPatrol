using System.Collections;
using System.Collections.Generic;
using Enemy;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private List<EnemyController> AllEnemies;
    [SerializeField] private GameObject m_SpawnParticles;
    [SerializeField] private float m_InitialSpawnInterval = 3f;
    [SerializeField] private float m_MinSpawnInterval = 0.8f;
    [SerializeField] private float m_SpawnDelay = 0.5f;
    [SerializeField] private int m_MaxEnemies = 15;
    [SerializeField] private float m_EnemySpacing = 1.5f; // Min distance between enemies

    private float m_SpawnTimer;
    private float m_ElapsedTime;
    private List<EnemyController> m_ActiveEnemies = new List<EnemyController>();

    private void Update()
    {
        // Remove nulls (killed enemies)
        m_ActiveEnemies.RemoveAll(e => e == null);
        m_ElapsedTime += Time.deltaTime;

        if (m_ActiveEnemies.Count >= m_MaxEnemies)
            return;

        m_SpawnTimer -= Time.deltaTime;
        if (m_SpawnTimer <= 0f)
        {
            TrySpawnEnemy();
            m_SpawnTimer = Mathf.Max(m_InitialSpawnInterval - m_ElapsedTime / 30f, m_MinSpawnInterval); // Speed up over time
        }
    }

    private void TrySpawnEnemy()
    {
        Vector3 spawnPosition;
        bool foundPosition = false;

        // Try 10 times to find a valid non-overlapping spawn position
        for (int i = 0; i < 10; i++)
        {
            spawnPosition = PickRandomPosition();
            if (!Physics.CheckSphere(spawnPosition, m_EnemySpacing))
            {
                StartCoroutine(SpawnEnemyWithEffect(spawnPosition));
                foundPosition = true;
                break;
            }
        }

        if (!foundPosition)
        {
            Debug.Log("Couldn't find valid spawn position");
        }
    }

    private IEnumerator SpawnEnemyWithEffect(Vector3 spawnPosition)
    {
        Instantiate(m_SpawnParticles, spawnPosition, Quaternion.identity);
        yield return new WaitForSeconds(m_SpawnDelay);

        int difficultyIndex = GetDifficultyIndex();
        EnemyController enemyPrefab = AllEnemies[Random.Range(0, difficultyIndex + 1)];

        EnemyController enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.transform.parent = transform;
        m_ActiveEnemies.Add(enemy);
    }

    private int GetDifficultyIndex()
    {
        float t = Mathf.Clamp01(m_ElapsedTime / 120f); // Over 2 minutes, unlock all enemies
        return Mathf.Clamp(Mathf.FloorToInt(t * (AllEnemies.Count - 1)), 0, AllEnemies.Count - 1);
    }

    private Vector3 PickRandomPosition()
    {
        float x = Random.Range(-Constants.EnvironmentConstants.SpawnWidth, Constants.EnvironmentConstants.SpawnWidth);
        float y = Random.Range(-Constants.EnvironmentConstants.SpawnHeight, Constants.EnvironmentConstants.SpawnHeight);
        return new Vector3(x, y, 0); // y=0 for 2D or ground level in 3D
    }
}
