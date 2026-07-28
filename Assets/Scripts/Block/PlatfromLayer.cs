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

        [Tooltip("Gizmo color for this layer's debug markers.")]
        public Color gizmoColor = Color.white;

        [System.NonSerialized] public readonly PlatformLayerRuntime runtime = new PlatformLayerRuntime();

        public PlatformBlock[] ResolvePrefabPool(PlatformBlock[] fallback) =>
            (prefabOverride != null && prefabOverride.Length > 0) ? prefabOverride : fallback;
    }
}