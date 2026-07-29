using System.Collections.Generic;
using UnityEngine;
using Valley.Core.Pooling;

namespace Valley.Level.Obstacles
{
    /// <summary>
    /// Spawns global obstacles (lasers, missiles, etc) that aren't tied to any specific platform. Picks
    /// a random prefab, waits for a free slot under maxActiveObstacles, hands it the player reference,
    /// and calls BeginAnticipation() - the obstacle drives its own lifecycle from there and reports back
    /// through its Despawned event when it's done, at which point it's released to the pool and its
    /// slot frees up. The next spawn timer is a fresh random value within spawnIntervalRange each time.
    /// </summary>
    public class UniversalObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        [Tooltip("Candidate obstacle prefabs. One is picked uniformly at random each spawn.")]
        public ObstacleEntity[] obstaclePrefabs;

        [Header("Global Limit")]
        [Tooltip("Maximum number of these obstacles allowed active at once, across all prefab types.")]
        public int maxActiveObstacles = 2;

        [Header("Spawn Timing")]
        [Tooltip("Min/max seconds between spawns. A new random value in this range is picked after every spawn.")]
        public Vector2 spawnIntervalRange = new Vector2(3f, 6f);

        [Header("Debug Gizmos")]
        public bool showGizmos = true;

        readonly List<ObstacleEntity> activeObstacles = new List<ObstacleEntity>();
        PrefabPoolGroup<ObstacleEntity> pool;
        float spawnTimer;

        void Awake()
        {
            pool = new PrefabPoolGroup<ObstacleEntity>(transform);
            spawnTimer = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        }

        void Update()
        {
            if (player == null || obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;
            if (activeObstacles.Count >= maxActiveObstacles) return;

            SpawnObstacle();
            spawnTimer = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        }

        void SpawnObstacle()
        {
            ObstacleEntity prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            ObstacleEntity instance = pool.Get(prefab);
            instance.player = player;

            instance.Despawned -= HandleObstacleDespawned;
            instance.Despawned += HandleObstacleDespawned;

            activeObstacles.Add(instance);
            instance.BeginAnticipation();
        }

        void HandleObstacleDespawned(ObstacleEntity instance)
        {
            activeObstacles.Remove(instance);
            pool.Release(instance);
        }

        void OnDrawGizmos()
        {
            if (!showGizmos || player == null) return;

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(player.position, 0.4f);

#if UNITY_EDITOR
            int liveCount = Application.isPlaying ? activeObstacles.Count : 0;
            UnityEditor.Handles.Label(player.position + Vector3.up * 0.6f, $"Obstacles: {liveCount}/{maxActiveObstacles}");
#endif
        }
    }
}