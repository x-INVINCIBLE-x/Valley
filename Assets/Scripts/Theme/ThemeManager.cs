using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valley.Theming
{
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }

        public static event Action<ThemeDefinition> OnThemeChanged;
        public static event Action<ThemeDefinition> OnThemePurchased;

        [SerializeField] private ThemeDefinition initialTheme;
        [SerializeField] private ThemeDefinition[] availableThemes;

        public ThemeDefinition CurrentTheme { get; private set; }
        public IReadOnlyList<ThemeDefinition> AvailableThemes => availableThemes;
        public IReadOnlyCollection<ThemeDefinition> OwnedThemes => _ownedThemes;

        private readonly HashSet<ThemeDefinition> _ownedThemes = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (initialTheme != null)
            {
                CurrentTheme = initialTheme;
                _ownedThemes.Add(initialTheme);
            }
        }

        private void Start()
        {
            if (CurrentTheme != null)
                OnThemeChanged?.Invoke(CurrentTheme);
        }

        public void SetTheme(ThemeDefinition theme)
        {
            if (theme == null || theme == CurrentTheme)
                return;

            CurrentTheme = theme;
            OnThemeChanged?.Invoke(theme);
        }

        public bool IsOwned(ThemeDefinition theme)
        {
            return theme != null && _ownedThemes.Contains(theme);
        }

        public void MarkThemeOwned(ThemeDefinition theme)
        {
            if (theme == null)
                return;

            if (_ownedThemes.Add(theme))
                OnThemePurchased?.Invoke(theme);
        }

        public ThemeDefinition GetThemeById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (var theme in availableThemes)
            {
                if (theme != null && theme.SaveId == id)
                    return theme;
            }

            return null;
        }
    }
}