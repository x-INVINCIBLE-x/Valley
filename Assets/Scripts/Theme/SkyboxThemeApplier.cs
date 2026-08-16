using UnityEngine;

namespace Valley.Theming
{
    public class SkyboxThemeApplier : ThemeableBehaviour
    {
        [SerializeField] private bool updateAmbientLighting = true;

        protected override void ApplyTheme(ThemeDefinition theme)
        {
            if (theme.skyboxMaterial == null) return;

            RenderSettings.skybox = theme.skyboxMaterial;

            if (updateAmbientLighting)
            {
                RenderSettings.ambientLight = theme.ambientLightColor;
                RenderSettings.ambientIntensity = theme.ambientIntensity;
            }

            // Refreshes ambient lighting/reflections to match the new skybox
            DynamicGI.UpdateEnvironment();
        }
    }
}