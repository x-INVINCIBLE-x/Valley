using System.Collections.Generic;
using UnityEngine;

namespace Valley.Level.Obstacles
{
    /// <summary>
    /// One obstacle prefab within a category. Picked by weight among the category's still-spawnable
    /// entries; once it's been picked maxSpawnsBeforeReset times it's excluded from picks until its
    /// whole category resets (see ObstacleCategory.AllExhausted/ResetUsage), independently of
    /// maxActiveInstances, which caps how many of THIS prefab can be alive at once regardless of usage
    /// count.
    /// </summary>
    [System.Serializable]
    public class ObstacleEntry
    {
        public ObstacleEntity prefab;

        [Tooltip("Relative chance this entry is picked among the category's other still-spawnable entries.")]
        [Range(0.01f, 10f)] public float weight = 1f;

        [Tooltip("How many times this entry can be picked before it's excluded until the category resets.")]
        public int maxSpawnsBeforeReset = 3;

        [Tooltip("Max concurrent live instances of this specific prefab, independent of maxSpawnsBeforeReset.")]
        public int maxActiveInstances = 1;

        [System.NonSerialized] public int usesSinceReset;
        [System.NonSerialized] public int liveCount;

        public bool IsSpawnable => usesSinceReset < maxSpawnsBeforeReset && liveCount < maxActiveInstances;
    }

    /// <summary>
    /// A group of related obstacle entries (e.g. "Lasers", "Missiles"). Categories are picked by weight
    /// among those with at least one spawnable entry and room under the spawner's maxActiveCategories.
    /// Once every entry in a category has used up its maxSpawnsBeforeReset, the whole category's usage
    /// counters reset so it can be drawn from again.
    /// </summary>
    [System.Serializable]
    public class ObstacleCategory
    {
        public string categoryName = "Category";

        [Tooltip("Relative chance this category is picked among other eligible categories.")]
        [Range(0.01f, 10f)] public float categoryWeight = 1f;

        public List<ObstacleEntry> obstacles = new List<ObstacleEntry>();

        [System.NonSerialized] public int liveCount;

        public bool HasSpawnableObstacle()
        {
            foreach (var entry in obstacles)
                if (entry.IsSpawnable) return true;
            return false;
        }

        public bool AllExhausted()
        {
            foreach (var entry in obstacles)
                if (entry.usesSinceReset < entry.maxSpawnsBeforeReset) return false;
            return true;
        }

        public void ResetUsage()
        {
            foreach (var entry in obstacles) entry.usesSinceReset = 0;
        }
    }
}