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
            ThemeManager.OnThemeChanged += ApplyTheme;

            if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
            {
                ApplyTheme(ThemeManager.Instance.CurrentTheme);
            }
        }

        protected virtual void OnDisable()
        {
            if (mode == ThemingMode.Enable)
                ThemeManager.OnThemeChanged -= ApplyTheme;
        }

        public virtual void OnDestroy()
        {
            if (mode == ThemingMode.Start)
                ThemeManager.OnThemeChanged -= ApplyTheme;
        }

        protected abstract void ApplyTheme(ThemeDefinition theme);
    }
}
