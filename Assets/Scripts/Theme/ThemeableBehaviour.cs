using UnityEngine;

namespace Valley.Theming
{
    public abstract class ThemeableBehaviour : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            ThemeManager.OnThemeChanged += ApplyTheme;

            if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
            {
                ApplyTheme(ThemeManager.Instance.CurrentTheme);
            }
        }

        protected virtual void OnDisable()
        {
            ThemeManager.OnThemeChanged -= ApplyTheme;
        }

        protected abstract void ApplyTheme(ThemeDefinition theme);
    }
}
