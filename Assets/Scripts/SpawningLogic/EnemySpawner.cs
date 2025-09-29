using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Enemy;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpawningLogic
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private List<EnemyController> AllEnemies;
        [Space]
        [SerializeField] private GameObject m_SpawnParticles;
        [SerializeField] private float m_SpawnDelay = .8f;

        private Dictionary<EnemyType, EnemyController> _allEnemiesMap;

        private void Awake()
        {
            _allEnemiesMap = AllEnemies.ToDictionary(e => e.EnemyType, e => e);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            EnemyController enemyToSpawnPrefab = AllEnemies[Random.Range(0, AllEnemies.Count)];
            Vector3 spawnPosition = PickRandomPosition();
            Instantiate(m_SpawnParticles, spawnPosition, Quaternion.identity);
            DOVirtual.DelayedCall(m_SpawnDelay, () =>
            {
                EnemyController enemy = Instantiate(enemyToSpawnPrefab, spawnPosition, Quaternion.identity);
                enemy.transform.parent = transform;
            });
        }

        private Vector3 PickRandomPosition()
        {
            float x = Random.Range(-Constants.EnvironmentConstants.SpawnWidth, Constants.EnvironmentConstants.SpawnWidth);
            float z = Random.Range(-Constants.EnvironmentConstants.SpawnHeight, Constants.EnvironmentConstants.SpawnHeight);
            return new Vector3(x, z);
        }
    }
}