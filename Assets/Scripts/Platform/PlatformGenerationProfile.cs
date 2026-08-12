using UnityEngine;
using Valley.Core;

namespace Valley.Level.Generation
{
    /// <summary>
    /// A swappable "data set" for <see cref="PlatformChunkSpawner"/>: everything that defines what the
    /// mid (traversal) layer generates and how hard or forgiving it is. Assign one as a spawner's
    /// <see cref="PlatformChunkSpawner.initialProfile"/> to use it from the very first seeded platform,
    /// or reference it from a <see cref="PlatformChunkSpawner.PlatformProgressionStage"/> to switch to it
    /// once a distance threshold is crossed mid-run.
    ///
    /// Applying a profile only changes what gets generated FROM THAT POINT ON - it never touches
    /// platforms that already exist, so history stays reproducible exactly like the rest of this spawner.
    ///
    /// Deliberately does NOT cover side layers (the up/down decorative layers) or spawner infrastructure
    /// (player reference, history retention, debug gizmos) - those stay on the spawner itself and are
    /// unaffected by profile swaps. Side layers DO still pick up a new global prefab pool automatically
    /// (any layer without its own prefab-pool override falls back to platformPrefabs), so re-theming the
    /// mid layer re-themes most side content for free.
    /// </summary>
    [CreateAssetMenu(fileName = "PlatformGenerationProfile", menuName = "Valley/Level Generation/Platform Generation Profile")]
    public class PlatformGenerationProfile : ScriptableObject
    {
        [Header("Prefab Pool")]
        public PlatformBlock[] platformPrefabs;

        [Header("Reachability")]
        [Tooltip("Should mirror the player's actual forward-run speed.")]
        public float forwardSpeed = 6f;
        [Tooltip("The same LaunchProfile asset driving the player's launch motor.")]
        public LaunchProfile launchProfile;
        public float gravity = 20f;
        public int maxLaunches = 2;
        [Tooltip("Distance/height subtracted from the computed max reach so imperfect player timing still lands.")]
        public float reachabilitySafetyMargin = 0.5f;

        [Header("Spawn Window")]
        public float spawnAheadDistance = 30f;
        public float despawnBehindDistance = 15f;

        [Header("Vertical Band (Mid Layer)")]
        [Tooltip("Platforms never generate above (player's current Y + this offset).")]
        public float upperBoundOffset = 6f;
        [Tooltip("Max |change in edge height| allowed between two consecutive mid-layer platforms. There is deliberately no matching lower clamp.")]
        public float maxVerticalStep = 3f;

        [Header("Gap Control")]
        public float minGapX = 0.5f;
        [Tooltip("Authored ceiling on mid-layer gap size; still further clamped by reachability.")]
        public float maxGapX = 4f;
        [Range(0f, 1f)] public float gapChance = 0.65f;

        [Header("Depth Variance (Z Noise)")]
        [Tooltip("Random Z offset added on top of the spawner's own Z position. Leave both at 0 to keep a fixed Z.")]
        public float zNoiseMin = 0f;
        public float zNoiseMax = 0f;

        [Header("Score-Based Spawning")]
        [Tooltip("How many times a candidate's spawnChance is allowed to fail in a row before the last-picked candidate is placed anyway, guaranteeing forward progress.")]
        public int maxSpawnChanceAttempts = 4;

        [Header("Anti-Runaway Safety (Mid Layer)")]
        [Tooltip("Hard cap on flush attaches in a row, so blocks that allow sticking can't chain into an endless floor.")]
        public int maxConsecutiveSticks = 3;
        [Tooltip("After this many near-max-difficulty gaps in a row, the next gap eases off.")]
        public int maxConsecutiveHardGaps = 2;
        [Tooltip("Every N newly-generated platforms, force a flat, unrotated, easy gap as a guaranteed-reachable checkpoint.")]
        public int guaranteedSafetyInterval = 8;
    }
}