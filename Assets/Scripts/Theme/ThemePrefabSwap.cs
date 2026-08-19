using System;
using UnityEngine;

namespace Valley.Theming
{
    public class ThemePrefabSwap : ThemeableBehaviour
    {
        [Serializable]
        private struct WeightedPrefab
        {
            public GameObject prefab;

            [Tooltip("Relative chance of this prefab being picked. " +
                "Weights are normalized against the sum of all weights in the entry," +
                "so they don't need to add up to any particular total.")]
            [Min(0f)]
            public float weight;
        }

        [Serializable]
        private struct Entry
        {
            public ThemeDefinition theme;

            [Tooltip("Candidates for this theme. One is chosen at random, weighted by each candidate's weight.")]
            public WeightedPrefab[] prefabs;
        }

        [Tooltip("One entry per theme this object cares about. A weighted-random prefab is picked from the matching entry, and the spawned instance replaces whatever this script previously spawned, as a child of this transform.")]
        [SerializeField] private Entry[] entries;

        private GameObject _spawnedInstance;

        protected override void ApplyTheme(ThemeDefinition theme)
        {
            foreach (var entry in entries)
            {
                if (entry.theme != theme) continue;

                var prefab = PickWeightedPrefab(entry.prefabs);
                if (prefab == null)
                {
                    Debug.LogWarning($"[{nameof(ThemePrefabSwap)}] No valid prefab candidates for theme '{theme}' on '{name}'.", this);
                    return;
                }

                if (_spawnedInstance != null) Destroy(_spawnedInstance);
                _spawnedInstance = Instantiate(prefab,
                                               transform.position,
                                               prefab.transform.rotation,
                                               transform);
                return;
            }
        }

        private static GameObject PickWeightedPrefab(WeightedPrefab[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0) return null;

            float totalWeight = 0f;
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i].prefab == null) continue;
                totalWeight += Mathf.Max(0f, prefabs[i].weight);
            }

            if (totalWeight <= 0f)
            {
                int validCount = 0;
                for (int i = 0; i < prefabs.Length; i++)
                {
                    if (prefabs[i].prefab != null) validCount++;
                }
                if (validCount == 0) return null;

                int nth = UnityEngine.Random.Range(0, validCount);
                int seen = 0;
                for (int i = 0; i < prefabs.Length; i++)
                {
                    if (prefabs[i].prefab == null) continue;
                    if (seen == nth) return prefabs[i].prefab;
                    seen++;
                }
                return null;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i].prefab == null) continue;
                cumulative += Mathf.Max(0f, prefabs[i].weight);
                if (roll <= cumulative) return prefabs[i].prefab;
            }

            for (int i = prefabs.Length - 1; i >= 0; i--)
            {
                if (prefabs[i].prefab != null) 
                    return prefabs[i].prefab;
            }
            return null;
        }
    }
}