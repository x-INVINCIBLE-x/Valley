using System;
using System.Linq;
using UnityEngine;
using Valley.Level.Generation;
using Valley.Level.Spawning;
using Valley.Scoring;

namespace Valley.Level.Difficulty
{
    /// <summary>
    /// Watches a DistanceScoreTracker's Score and, whenever it crosses into a new tier, applies that
    /// tier's overrides directly onto the referenced PlatformBlock prefabs' spawnChance/spawnWeight and
    /// onto the referenced PlatformSpawnPointGenerators' categories (matched by categoryName). A tier
    /// only needs entries for what actually changes at that threshold.
    /// </summary>
    public class ScoreThresholdContentController : MonoBehaviour
    {
        [Header("Score Source")]
        public DistanceScoreTracker distanceTracker;

        [Header("Targets")]
        [Tooltip("Every generator whose categories should be affected by a tier's spawnPointOverrides - typically one per distinct platform prefab that has spawn points.")]
        public PlatformSpawnPointGenerator[] spawnPointGenerators;

        [Header("Tiers")]
        [Tooltip("Sorted by scoreThreshold automatically at startup - author them in any order.")]
        public ScoreTier[] tiers = new ScoreTier[0];

        int currentTierIndex = -1;

        [Serializable]
        public struct PlatformOverride
        {
            public PlatformBlock prefab;
            [Range(0f, 1f)] public float spawnChance;
            public float spawnWeight;
        }

        [Serializable]
        public struct SpawnPointCategoryOverride
        {
            [Tooltip("Matched against SpawnPointCategory.categoryName on every generator in spawnPointGenerators.")]
            public string categoryName;
            public int activeCount;
            [Range(0f, 1f)] public float activationProbability;
            [Tooltip("If assigned (non-empty), replaces this category's spawnable prefabs at this tier.")]
            public SpawnedEntity[] prefabOverride;
        }

        [Serializable]
        public class ScoreTier
        {
            public float scoreThreshold;
            public PlatformOverride[] platformOverrides = new PlatformOverride[0];
            public SpawnPointCategoryOverride[] spawnPointOverrides = new SpawnPointCategoryOverride[0];
        }

        void Awake()
        {
            tiers = tiers.OrderBy(t => t.scoreThreshold).ToArray();
        }

        void Update()
        {
            if (distanceTracker == null || tiers.Length == 0) return;

            int targetTier = FindTierIndex(distanceTracker.Distance);
            if (targetTier < 0 || targetTier == currentTierIndex) return;

            ApplyTier(tiers[targetTier]);
            currentTierIndex = targetTier;
        }

        int FindTierIndex(float score)
        {
            int index = -1;
            for (int i = 0; i < tiers.Length; i++)
            {
                if (score >= tiers[i].scoreThreshold) index = i;
                else break;
            }
            return index;
        }

        void ApplyTier(ScoreTier tier)
        {
            foreach (var o in tier.platformOverrides)
            {
                if (o.prefab == null) continue;
                o.prefab.spawnChance = o.spawnChance;
                o.prefab.spawnWeight = o.spawnWeight;
            }

            if (spawnPointGenerators == null) return;

            foreach (var generator in spawnPointGenerators)
            {
                if (generator == null || generator.categories == null) continue;

                foreach (var categoryOverride in tier.spawnPointOverrides)
                {
                    var category = generator.categories.Find(c => c.categoryName == categoryOverride.categoryName);
                    if (category == null) continue;

                    category.activeCount = categoryOverride.activeCount;
                    category.activationProbability = categoryOverride.activationProbability;
                    if (categoryOverride.prefabOverride != null && categoryOverride.prefabOverride.Length > 0)
                        category.prefabs = categoryOverride.prefabOverride;
                }
            }
        }
    }
}