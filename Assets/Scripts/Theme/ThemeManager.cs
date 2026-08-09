using System;
using UnityEngine;

namespace Valley.Theming
{
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }
        public static event Action<ThemeDefinition> OnThemeChanged;

        [SerializeField] private ThemeDefinition initialTheme;

        public ThemeDefinition CurrentTheme { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (initialTheme != null) SetTheme(initialTheme);
        }

        public void SetTheme(ThemeDefinition theme)
        {
            if (theme == null || theme == CurrentTheme) return;

            CurrentTheme = theme;
            OnThemeChanged?.Invoke(theme);
        }
    }
}
