using System.Collections.Generic;
using UnityEngine;
using Valley.Core.Pooling;

namespace Valley.Level.Obstacles
{
    /// <summary>
    /// Spawns global obstacles organized into categories. Categories activate in a staggered round-robin:
    /// up to maxActiveCategories can run concurrently, a new one is brought online roughly every
    /// categoryActivationDelayRange seconds (ramping from 1 up to the max rather than bursting all at
    /// once), and once a category has been activated it's excluded from being picked again until every
    /// other eligible category has also been picked - at which point the whole round resets. While
    /// active, a category spawns consecutiveSpawnCount obstacles in a row (each picked by weight among
    /// its still-spawnable entries), consecutiveSpawnDelay apart, then its cycle ends and its
    /// concurrent-category slot frees up for the next one.
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
        [Tooltip("Maximum number of categories allowed to be running a consecutive-spawn cycle at the same time.")]
        public int maxActiveCategories = 2;

        [Header("Category Activation")]
        [Tooltip("Delay range before trying to bring another category cycle online. Re-rolled every time a new category is actually activated.")]
        public Vector2 categoryActivationDelayRange = new Vector2(2f, 4f);

        [Header("Debug Gizmos")]
        public bool showGizmos = true;

        readonly List<ObstacleEntity> activeObstacles = new List<ObstacleEntity>();
        readonly Dictionary<ObstacleEntity, ObstacleCategory> activeInstanceCategory = new Dictionary<ObstacleEntity, ObstacleCategory>();
        readonly Dictionary<ObstacleEntity, ObstacleEntry> activeInstanceEntry = new Dictionary<ObstacleEntity, ObstacleEntry>();
        readonly List<CategoryCycle> runningCycles = new List<CategoryCycle>();

        PrefabPoolGroup<ObstacleEntity> pool;
        float categoryActivationTimer;

        class CategoryCycle
        {
            public ObstacleCategory category;
            public int spawnsRemaining;
            public float nextSpawnTimer;
        }

        void Awake()
        {
            pool = new PrefabPoolGroup<ObstacleEntity>(transform);
            categoryActivationTimer = Random.Range(categoryActivationDelayRange.x, categoryActivationDelayRange.y);
        }

        void Update()
        {
            if (player == null || categories == null || categories.Count == 0) return;

            UpdateRunningCycles();
            TryActivateNewCategory();
        }

        void UpdateRunningCycles()
        {
            for (int i = runningCycles.Count - 1; i >= 0; i--)
            {
                CategoryCycle cycle = runningCycles[i];
                cycle.nextSpawnTimer -= Time.deltaTime;
                if (cycle.nextSpawnTimer > 0f) continue;
                if (activeObstacles.Count >= maxActiveObstacles) continue;

                ObstacleEntry entry = ChooseEligibleObstacle(cycle.category);
                if (entry == null)
                {
                    runningCycles.RemoveAt(i);
                    continue;
                }

                SpawnFrom(cycle.category, entry);
                cycle.spawnsRemaining--;
                cycle.nextSpawnTimer = cycle.category.consecutiveSpawnDelay;

                if (cycle.spawnsRemaining <= 0)
                    runningCycles.RemoveAt(i);
            }
        }

        void TryActivateNewCategory()
        {
            categoryActivationTimer -= Time.deltaTime;
            if (categoryActivationTimer > 0f) return;
            if (runningCycles.Count >= maxActiveCategories) return;

            ObstacleCategory category = ChooseCategoryForNewCycle();
            if (category == null) return;

            runningCycles.Add(new CategoryCycle
            {
                category = category,
                spawnsRemaining = Mathf.Max(1, category.consecutiveSpawnCount),
                nextSpawnTimer = 0f
            });
            category.usedThisRound = true;

            categoryActivationTimer = Random.Range(categoryActivationDelayRange.x, categoryActivationDelayRange.y);
        }

        ObstacleCategory ChooseCategoryForNewCycle()
        {
            var eligible = new List<ObstacleCategory>();
            float totalWeight = 0f;

            foreach (var category in categories)
            {
                if (category.usedThisRound) continue;
                if (IsCategoryRunning(category)) continue;
                if (!category.HasSpawnableObstacle()) continue;

                eligible.Add(category);
                totalWeight += category.categoryWeight;
            }

            if (eligible.Count > 0)
                return PickWeighted(eligible, c => c.categoryWeight, totalWeight);

            if (!AllEligibleCategoriesUsed()) return null;

            ResetRound();

            foreach (var category in categories)
            {
                if (category.usedThisRound) continue;
                if (IsCategoryRunning(category)) continue;
                if (!category.HasSpawnableObstacle()) continue;

                eligible.Add(category);
                totalWeight += category.categoryWeight;
            }

            return eligible.Count > 0 ? PickWeighted(eligible, c => c.categoryWeight, totalWeight) : null;
        }

        bool IsCategoryRunning(ObstacleCategory category)
        {
            foreach (var cycle in runningCycles)
                if (cycle.category == category) return true;
            return false;
        }

        bool AllEligibleCategoriesUsed()
        {
            bool anyEligibleAtAll = false;
            foreach (var category in categories)
            {
                if (!category.HasSpawnableObstacle()) continue;
                anyEligibleAtAll = true;
                if (!category.usedThisRound) return false;
            }
            return anyEligibleAtAll;
        }

        void ResetRound()
        {
            foreach (var category in categories)
                category.usedThisRound = false;
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
            int runningCategoryCount = Application.isPlaying ? runningCycles.Count : 0;
            UnityEditor.Handles.Label(player.position + Vector3.up * 0.6f,
                $"Obstacles: {liveCount}/{maxActiveObstacles}   Categories: {runningCategoryCount}/{maxActiveCategories}");
#endif
        }
    }
}