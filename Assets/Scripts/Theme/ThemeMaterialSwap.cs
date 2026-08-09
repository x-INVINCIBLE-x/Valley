using System;
using UnityEngine;

namespace Valley.Theming
{
    [RequireComponent(typeof(Renderer))]
    public class ThemeMaterialSwap : ThemeableBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public ThemeDefinition theme;
            public Material material;
        }

        [Tooltip("One entry per theme this object cares about. Themes with no entry keep whatever material was last applied.")]
        [SerializeField] private Entry[] entries;

        private Renderer _renderer;

        private void Awake() => _renderer = GetComponent<Renderer>();

        protected override void ApplyTheme(ThemeDefinition theme)
        {
            foreach (var entry in entries)
            {
                if (entry.theme != theme) continue;

                _renderer.material = entry.material;
                return;
            }
        }
    }
}
