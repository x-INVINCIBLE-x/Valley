using UnityEngine;

namespace Valley.Theming
{
    public abstract class ThemeableBehaviour : MonoBehaviour
    {
        public enum ThemingMode
        {
            Enable,
            Start
        }

        [SerializeField] private ThemingMode mode = ThemingMode.Enable;

        private bool _isSubscribed;
        private ThemeDefinition _appliedTheme;

        protected virtual void OnEnable()
        {
            if (mode == ThemingMode.Enable)
                Setup();
        }

        protected virtual void Start()
        {
            if (mode == ThemingMode.Start)
                Setup();
        }

        private void Setup()
        {
            if (!_isSubscribed)
            {
                ThemeManager.OnThemeChanged += HandleThemeChanged;
                _isSubscribed = true;
            }

            ThemeDefinition currentTheme = ThemeManager.Instance != null
                ? ThemeManager.Instance.CurrentTheme
                : null;

            if (currentTheme != null &&
                _appliedTheme != currentTheme)
            {
                ApplyTheme(currentTheme);
                _appliedTheme = currentTheme;
            }
        }

        private void HandleThemeChanged(ThemeDefinition theme)
        {
            if (theme == null)
                return;

            if (_appliedTheme == theme)
                return;

            ApplyTheme(theme);
            _appliedTheme = theme;
        }

        protected virtual void OnDisable()
        {
            if (!_isSubscribed)
                return;

            ThemeManager.OnThemeChanged -= HandleThemeChanged;
            _isSubscribed = false;
        }

        protected virtual void OnDestroy()
        {
            if (!_isSubscribed)
                return;

            ThemeManager.OnThemeChanged -= HandleThemeChanged;
            _isSubscribed = false;
        }

        protected abstract void ApplyTheme(ThemeDefinition theme);
    }
}