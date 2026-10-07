using UnityEngine;

namespace Valley.Theming
{
    public class ThemeGameObjectActivator : ThemeableBehaviour
    {
        [SerializeField] private ThemeDefinition[] activeForThemes;

        [Tooltip("GameObject to enable/disable. If empty, this GameObject will be used.")]
        [SerializeField] private GameObject targetGameObject;

        protected override void ApplyTheme(ThemeDefinition theme)
        {
            if (theme == null)
                return;

            GameObject target = targetGameObject != null
                ? targetGameObject
                : gameObject;

            for (int i = 0; i < activeForThemes.Length; i++)
            {
                if (activeForThemes[i] == theme)
                {
                    target.SetActive(true);
                    return;
                }
            }

            target.SetActive(false);
        }
    }
}