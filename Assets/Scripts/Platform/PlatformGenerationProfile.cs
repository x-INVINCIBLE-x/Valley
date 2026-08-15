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
    /// Side layers (the up/down decorative layers) are covered too - including adding/removing layers and
    /// each layer's own prefab-pool override - see <see cref="sideLayers"/> below for exactly how. Spawner
    /// infrastructure (player reference, history retention, debug gizmos) always stays on the spawner
    /// itself, unaffected by profile swaps.
    /// </summary>
    [CreateAssetMenu(fileName = "PlatformGenerationProfile", menuName = "Valley/Level Generation/Platform Generation Profile")]
    public class PlatformGenerationProfile : ScriptableObject
    {
        /// <summary>
        /// Raised whenever any field on this asset changes in the Inspector. PlatformChunkSpawner
        /// subscribes to this for whatever profile is assigned to its (editor-only) testProfile slot, so
        /// tweaking values here re-applies them to a running spawner immediately - see
        /// PlatformChunkSpawner.testProfile. OnValidate is never invoked in a player build, so in practice
        /// this event never fires outside the editor.
        /// </summary>
        public event System.Action Changed;

        void OnValidate() => Changed?.Invoke();

        /// <summary>
        /// Mirrors every tunable field on PlatformLayer (label, verticalOffset, verticalJitter,
        /// gapMultiplier, stickChanceBonus, maxConsecutiveSticks, clampToReachability, prefabOverride,
        /// gizmoColor) - deliberately NOT a PlatformLayer itself, since a PlatformLayer also carries live
        /// runtime state (pooled instances, generation history) that must never live inside a shared
        /// ScriptableObject asset.
        /// </summary>
        [System.Serializable]
        public struct SideLayerConfig
        {
            public string label;
            public float verticalOffset;
            public float verticalJitter;
            public float gapMultiplier;
            public float stickChanceBonus;
            public int maxConsecutiveSticks;
            public bool clampToReachability;
            [Tooltip("Optional per-layer prefab set. Leave empty to reuse this profile's platformPrefabs list.")]
            public PlatformBlock[] prefabOverride;
            public Color gizmoColor;
        }

        [Header("Prefab Pool")]
        public PlatformBlock[] platformPrefabs;

        /// <summary>Pairs a prefab with its spawn weight - see prefabWeights below.</summary>
        [System.Serializable]
        public struct PrefabWeight
        {
            public PlatformBlock prefab;
            [Range(0.01f, 10f)] public float weight;
        }

        [Tooltip("Spawn weight per prefab, matched BY PREFAB REFERENCE (not index) - so the same entry applies wherever that prefab shows up, in platformPrefabs or in any side layer's prefabOverride pool. A prefab not listed here uses the spawner's defaultPrefabWeight. This is what replaced each PlatformBlock describing its own weight: the same prefab can now be common in one profile and rare in another.")]
        public PrefabWeight[] prefabWeights;

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

        [Header("Side Layers")]
        [Tooltip("Matched to the spawner's existing sideLayers array BY INDEX when this profile is applied - entry 0 maps to sideLayers[0], entry 1 to sideLayers[1], and so on. An index that already exists on the spawner keeps its live runtime/history and just gets retuned in place. An index beyond the spawner's current count creates a brand-new layer (seeded fresh). Leaving this EMPTY leaves every side layer exactly as currently configured on the spawner - untouched, not removed - so profiles that don't care about side layers can't accidentally wipe them. Any NON-empty array becomes the complete side-layer set going forward: if it has fewer entries than the spawner currently has side layers, the extra existing ones are released and dropped.")]
        public SideLayerConfig[] sideLayers = new SideLayerConfig[0];
    }
}