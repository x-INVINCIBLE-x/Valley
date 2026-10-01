using UnityEngine;

namespace Valley.Theming
{
    public class FogThemeApplier : ThemeableBehaviour
    {
        protected override void ApplyTheme(ThemeDefinition theme)
        {
            RenderSettings.fog = theme.fogEnabled;

            if (!theme.fogEnabled)
                return;

            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogMode = theme.fogMode;

            switch (theme.fogMode)
            {
                case FogMode.Linear:
                    RenderSettings.fogStartDistance = theme.fogStartDistance;
                    RenderSettings.fogEndDistance = theme.fogEndDistance;
                    break;

                case FogMode.Exponential:
                case FogMode.ExponentialSquared:
                    RenderSettings.fogDensity = theme.fogDensity;
                    break;
            }
        }
    }
}