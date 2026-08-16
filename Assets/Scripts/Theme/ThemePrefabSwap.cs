using System;
using UnityEngine;

namespace Valley.Theming
{
    public class ThemePrefabSwap : ThemeableBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public ThemeDefinition theme;
            public GameObject prefab;
        }

        [Tooltip("One entry per theme this object cares about. The spawned instance replaces whatever this script previously spawned, as a child of this transform.")]
        [SerializeField] private Entry[] entries;

        private GameObject _spawnedInstance;

        protected override void ApplyTheme(ThemeDefinition theme)
        {
            foreach (var entry in entries)
            {
                if (entry.theme != theme) continue;

                if (_spawnedInstance != null) Destroy(_spawnedInstance);
                _spawnedInstance = Instantiate(entry.prefab,
                                               transform.position,
                                               entry.prefab.transform.rotation,
                                               transform);
                return;
            }
        }
    }
}
