using System.Collections.Generic;
using UnityEngine;

namespace Valley.Level.Generation
{
    [System.Serializable]
    public class PlatformLayer
    {
        [Tooltip("Shown in the inspector list only, e.g. 'Up 2', 'Down 1'.")]
        public string label = "Layer";

        [Tooltip("Vertical offset from the mid layer's current edge height. Positive = above, negative = below.")]
        public float verticalOffset = 0f;

        [Tooltip("How far this layer's target height wanders above/below its offset baseline on each spawn.")]
        public float verticalJitter = 1f;

        [Tooltip("Multiplies the spawner's minGapX/maxGapX for this layer. >1 = sparser (harder to use), <1 = denser (blocks sit closer together).")]
        public float gapMultiplier = 1f;

        [Tooltip("Extra chance (0-1), added on top of each block's own attach success rate, when rolling a flush stick on this layer. Push this up on the lower layers so they trend toward a near-solid run.")]
        [Range(0f, 1f)] public float stickChanceBonus = 0f;

        [Tooltip("Caps consecutive flush attaches. Set high on layers meant to look near-solid (e.g. the bottom safety-net layers) - unlike the mid layer, an unbroken run here isn't a problem since the player isn't required to traverse it.")]
        public int maxConsecutiveSticks = 3;

        [Tooltip("If true, this layer's target height is also clamped by the launch reachability envelope, same as the mid layer. Leave off for layers that aren't meant to be a guaranteed-reachable path.")]
        public bool clampToReachability = false;

        [Tooltip("Optional per-layer prefab set. Leave empty to reuse the spawner's main platformPrefabs list.")]
        public PlatformBlock[] prefabOverride;

        [Tooltip("Optional per-layer prefab weights. Each entry biases how often that prefab is picked FOR THIS LAYER ONLY - the mid layer's profile weights/overrides never apply here, and this layer's weights never leak into the mid layer or any other side layer, even if they share the same prefab. A prefab with no entry here (and no runtime override via PlatformChunkSpawner.SetPrefabWeight(layer, prefab, weight)) falls back to the spawner's defaultPrefabWeight.")]
        public PrefabWeightEntry[] prefabWeights;

        [Tooltip("Gizmo color for this layer's debug markers.")]
        public Color gizmoColor = Color.white;

        [System.NonSerialized] public readonly PlatformLayerRuntime runtime = new PlatformLayerRuntime();

        /// <summary>Runtime overrides for this layer only, set via PlatformChunkSpawner.SetPrefabWeight(layer, prefab, weight). Take priority over prefabWeights and persist across profile swaps until ClearPrefabWeight(layer, prefab) is called.</summary>
        [System.NonSerialized] readonly Dictionary<PlatformBlock, float> weightOverrides = new Dictionary<PlatformBlock, float>();

        public PlatformBlock[] ResolvePrefabPool(PlatformBlock[] fallback) =>
            (prefabOverride != null && prefabOverride.Length > 0) ? prefabOverride : fallback;

        /// <summary>
        /// Resolves prefab's effective weight for this layer: a runtime override if one's been set via
        /// SetWeightOverride, else this layer's own prefabWeights entry for it, else defaultWeight (the
        /// spawner's defaultPrefabWeight). Independent of every other layer's weighting, mid included.
        /// </summary>
        public float GetPrefabWeight(PlatformBlock prefab, float defaultWeight)
        {
            if (prefab == null) return defaultWeight;
            if (weightOverrides.TryGetValue(prefab, out float overrideWeight)) return overrideWeight;

            if (prefabWeights != null)
            {
                foreach (var entry in prefabWeights)
                {
                    if (entry.prefab == prefab) return Mathf.Max(0f, entry.weight);
                }
            }

            return defaultWeight;
        }

        /// <summary>Overrides prefab's weight at runtime for this layer only. A weight of 0 excludes the prefab from being picked in this layer without removing it from prefabOverride.</summary>
        public void SetWeightOverride(PlatformBlock prefab, float weight)
        {
            if (prefab == null) return;
            weightOverrides[prefab] = Mathf.Max(0f, weight);
        }

        /// <summary>Removes a runtime override set via SetWeightOverride, reverting prefab to this layer's prefabWeights entry (or the spawner's defaultPrefabWeight if unlisted).</summary>
        public void ClearWeightOverride(PlatformBlock prefab)
        {
            if (prefab == null) return;
            weightOverrides.Remove(prefab);
        }
    }
}