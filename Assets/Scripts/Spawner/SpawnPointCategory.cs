using System.Collections.Generic;
using UnityEngine;

namespace Valley.Level.Spawning
{
    /// <summary>
    /// A single, manually-adjustable spawn slot belonging to a category. Auto-Generate creates these;
    /// dragging the Scene-view handle or editing localPosition directly both adjust the same point.
    /// </summary>
    [System.Serializable]
    public class SpawnPoint
    {
        [Tooltip("Position relative to the generator's transform.")]
        public Vector3 localPosition;

        [System.NonSerialized] public SpawnedEntity spawnedInstance;
    }

    /// <summary>
    /// One category of spawnable content on a platform (e.g. "Trap", "Collectible"). Owns a fixed set of
    /// spawn point slots; each time the platform is enabled, a random subset of size activeCount is
    /// selected as candidates, and each candidate independently rolls activationProbability before it
    /// actually spawns - so activeCount is a ceiling, not a guarantee.
    /// </summary>
    [System.Serializable]
    public class SpawnPointCategory
    {
        [Header("Identity")]
        [Tooltip("Shown in the inspector and as the Scene-view label for this category's points.")]
        public string categoryName = "Category";

        [Tooltip("Candidate prefabs for this category - one is picked uniformly at random per point that spawns.")]
        public SpawnedEntity[] prefabs;

        [Tooltip("Gizmo/handle color for this category's points.")]
        public Color gizmoColor = Color.white;

        [Header("Auto-Generate")]
        [Tooltip("How many spawn point slots 'Auto-Generate Spawn Points' creates for this category.")]
        [Range(0, 50)] public int autoPointCount = 5;

        [Tooltip("Random XY offset applied to each point when auto-generated.")]
        public Vector2 positionJitter = Vector2.zero;

        [Tooltip("Random Z range (min, max) applied to each point when auto-generated. Leave both at 0 to keep points on the platform's Z plane.")]
        public Vector2 zRange = Vector2.zero;

        [Header("Points")]
        [Tooltip("The actual spawn slots for this category. Auto-Generate fills this in, but points can be freely added, removed, or edited by hand too.")]
        public List<SpawnPoint> points = new List<SpawnPoint>();

        [Header("Activation")]
        [Tooltip("How many of the points above are selected as activation candidates each time the platform is (re)enabled.")]
        public int activeCount = 2;

        [Tooltip("Extra independent chance (0-1) for EACH selected candidate to actually spawn something. Even a fully-selected candidate can still whiff.")]
        [Range(0f, 1f)] public float activationProbability = 1f;
    }
}
