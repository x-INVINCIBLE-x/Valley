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
    /// among those not yet used this round and with at least one spawnable entry; once picked, a category
    /// is excluded from being picked again until every other eligible category has also been picked -
    /// at which point the whole round resets. While active, a category spawns consecutiveSpawnCount
    /// entries in a row, consecutiveSpawnDelay apart. Separately, once every entry in a category has used
    /// up its own maxSpawnsBeforeReset, that category's entry usage resets so it has fresh entries to draw
    /// from the next time it's picked.
    /// </summary>
    [System.Serializable]
    public class ObstacleCategory
    {
        public string categoryName = "Category";

        [Tooltip("Relative chance this category is picked among other categories not yet used this round.")]
        [Range(0.01f, 10f)] public float categoryWeight = 1f;

        public List<ObstacleEntry> obstacles = new List<ObstacleEntry>();

        [Header("Consecutive Spawn Burst")]
        [Tooltip("How many obstacles this category spawns in a row (each picked normally by weight among still-spawnable entries) once it's activated.")]
        public int consecutiveSpawnCount = 3;
        [Tooltip("Delay between each spawn within this category's consecutive burst.")]
        public float consecutiveSpawnDelay = 0.5f;

        [System.NonSerialized] public int liveCount;
        [System.NonSerialized] public bool usedThisRound;

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