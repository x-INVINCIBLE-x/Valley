using System;
using TMPro;
using UnityEngine;

namespace Valley.Theming
{
    [RequireComponent(typeof(TMP_Text))]
    public class ThemeTextColorSwap : ThemeableBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public ThemeDefinition theme;
            public Color color;
        }

        [Tooltip("One entry per theme this text cares about. Themes with no entry keep the last applied color.")]
        [SerializeField] private Entry[] entries;

        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        protected override void ApplyTheme(ThemeDefinition theme)
        {
            foreach (var entry in entries)
            {
                if (entry.theme != theme) continue;

                _text.color = entry.color;
                return;
            }
        }
    }
}