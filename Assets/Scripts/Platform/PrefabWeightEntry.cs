using UnityEngine;

namespace Valley.Level.Generation
{
    /// <summary>
    /// A single prefab + weight pairing. Used to build a layer's own local weight table (see
    /// <see cref="PlatformLayer.prefabWeights"/>) - a prefab with no matching entry in whatever table is
    /// being consulted falls back to PlatformChunkSpawner.defaultPrefabWeight.
    /// </summary>
    [System.Serializable]
    public struct PrefabWeightEntry
    {
        public PlatformBlock prefab;
        public float weight;
    }
}