using UnityEngine;

namespace Valley.Theming
{
    public class ThemeGameObjectActivator : ThemeableBehaviour
    {
        [SerializeField] private ThemeDefinition[] activeForThemes;

        protected override void ApplyTheme(ThemeDefinition theme)
        {
            if (theme == null)
                return;

            for (int i = 0; i < activeForThemes.Length; i++)
            {
                if (activeForThemes[i] == theme)
                {
                    gameObject.SetActive(true);
                    return;
                }
            }

            gameObject.SetActive(false);
        }
    }
}