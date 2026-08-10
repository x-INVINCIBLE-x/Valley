using System.Collections.Generic;
using UnityEngine;
using Valley.Core.Pooling;

namespace Valley.Level.Obstacles
{
    /// <summary>
    /// Spawns global obstacles (lasers, missiles, etc) that aren't tied to any specific platform,
    /// organized into weighted categories. Each spawn attempt: pick an eligible category by weight
    /// (respecting maxActiveCategories), pick an eligible entry within it by weight (respecting that
    /// entry's own maxActiveInstances and maxSpawnsBeforeReset), then hand the instance the player
    /// reference and call BeginAnticipation() - the obstacle drives its own lifecycle from there and
    /// reports back through its Despawned event when it's done, at which point it's released to the pool
    /// and its slot frees up on every cap it was counted against. The next spawn timer is a fresh random
    /// value within spawnIntervalRange each time a spawn actually happens.
    /// </summary>
    public class UniversalObstacleSpawner : MonoBehaviour
    {
        [Header("References")]
        public Transform player;

        [Header("Categories")]
        [Tooltip("Each category owns its own set of weighted, exhaustion-tracked obstacle entries.")]
        public List<ObstacleCategory> categories = new List<ObstacleCategory>();

        [Header("Global Limits")]
        [Tooltip("Maximum number of obstacles allowed active at once, across every category and prefab.")]
        public int maxActiveObstacles = 2;
        [Tooltip("Maximum number of distinct categories allowed to have a live obstacle at the same time. A category already represented among active obstacles doesn't count against this when picking its next one.")]
        public int maxActiveCategories = 2;

        [Header("Spawn Timing")]
        [Tooltip("Min/max seconds between spawns. A new random value in this range is picked after every successful spawn.")]
        public Vector2 spawnIntervalRange = new Vector2(3f, 6f);

        [Header("Debug Gizmos")]
        public bool showGizmos = true;

        readonly List<ObstacleEntity> activeObstacles = new List<ObstacleEntity>();
        readonly Dictionary<ObstacleEntity, ObstacleCategory> activeInstanceCategory = new Dictionary<ObstacleEntity, ObstacleCategory>();
        readonly Dictionary<ObstacleEntity, ObstacleEntry> activeInstanceEntry = new Dictionary<ObstacleEntity, ObstacleEntry>();

        PrefabPoolGroup<ObstacleEntity> pool;
        float spawnTimer;

        void Awake()
        {
            pool = new PrefabPoolGroup<ObstacleEntity>(transform);
            spawnTimer = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        }

        void Update()
        {
            if (player == null || categories == null || categories.Count == 0) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;
            if (activeObstacles.Count >= maxActiveObstacles) return;

            if (TrySpawnObstacle())
                spawnTimer = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        }

        bool TrySpawnObstacle()
        {
            ObstacleCategory category = ChooseEligibleCategory();
            if (category == null) return false;

            ObstacleEntry entry = ChooseEligibleObstacle(category);
            if (entry == null) return false;

            SpawnFrom(category, entry);
            return true;
        }

        ObstacleCategory ChooseEligibleCategory()
        {
            int activeCategoryCount = CountActiveCategories();

            var eligible = new List<ObstacleCategory>();
            float totalWeight = 0f;

            foreach (var category in categories)
            {
                if (!category.HasSpawnableObstacle()) continue;

                bool alreadyActive = category.liveCount > 0;
                if (!alreadyActive && activeCategoryCount >= maxActiveCategories) continue;

                eligible.Add(category);
                totalWeight += category.categoryWeight;
            }

            return PickWeighted(eligible, c => c.categoryWeight, totalWeight);
        }

        int CountActiveCategories()
        {
            int count = 0;
            foreach (var category in categories)
                if (category.liveCount > 0) count++;
            return count;
        }

        ObstacleEntry ChooseEligibleObstacle(ObstacleCategory category)
        {
            var eligible = new List<ObstacleEntry>();
            float totalWeight = 0f;

            foreach (var entry in category.obstacles)
            {
                if (!entry.IsSpawnable) continue;
                eligible.Add(entry);
                totalWeight += entry.weight;
            }

            return PickWeighted(eligible, e => e.weight, totalWeight);
        }

        static T PickWeighted<T>(List<T> candidates, System.Func<T, float> getWeight, float totalWeight)
        {
            if (candidates.Count == 0) return default;

            float roll = Random.Range(0f, totalWeight);
            float accum = 0f;
            foreach (var candidate in candidates)
            {
                accum += getWeight(candidate);
                if (roll <= accum) return candidate;
            }
            return candidates[candidates.Count - 1];
        }

        void SpawnFrom(ObstacleCategory category, ObstacleEntry entry)
        {
            ObstacleEntity instance = pool.Get(entry.prefab);
            instance.player = player;

            instance.Despawned -= HandleObstacleDespawned;
            instance.Despawned += HandleObstacleDespawned;

            activeObstacles.Add(instance);
            activeInstanceCategory[instance] = category;
            activeInstanceEntry[instance] = entry;

            entry.usesSinceReset++;
            entry.liveCount++;
            category.liveCount++;

            if (category.AllExhausted()) category.ResetUsage();

            instance.BeginAnticipation();
        }

        void HandleObstacleDespawned(ObstacleEntity instance)
        {
            activeObstacles.Remove(instance);
            pool.Release(instance);

            if (activeInstanceEntry.TryGetValue(instance, out var entry))
            {
                entry.liveCount = Mathf.Max(0, entry.liveCount - 1);
                activeInstanceEntry.Remove(instance);
            }

            if (activeInstanceCategory.TryGetValue(instance, out var category))
            {
                category.liveCount = Mathf.Max(0, category.liveCount - 1);
                activeInstanceCategory.Remove(instance);
            }
        }

        void OnDrawGizmos()
        {
            if (!showGizmos || player == null) return;

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(player.position, 0.4f);

#if UNITY_EDITOR
            int liveCount = Application.isPlaying ? activeObstacles.Count : 0;
            int liveCategoryCount = Application.isPlaying ? CountActiveCategories() : 0;
            UnityEditor.Handles.Label(player.position + Vector3.up * 0.6f,
                $"Obstacles: {liveCount}/{maxActiveObstacles}   Categories: {liveCategoryCount}/{maxActiveCategories}");
#endif
        }
    }
}