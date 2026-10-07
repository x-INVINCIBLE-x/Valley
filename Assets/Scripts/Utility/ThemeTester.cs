#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.InputSystem;
using Valley.Theming;

namespace Valley.EditorTools
{
    public class ThemeTester : MonoBehaviour
    {
        [SerializeField] private ThemeDefinition[] themes;

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                TrySetTheme(0);

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                TrySetTheme(1);

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                TrySetTheme(2);

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                TrySetTheme(3);

            if (Keyboard.current.digit5Key.wasPressedThisFrame)
                TrySetTheme(4);

            if (Keyboard.current.digit6Key.wasPressedThisFrame)
                TrySetTheme(5);

            if (Keyboard.current.digit7Key.wasPressedThisFrame)
                TrySetTheme(6);

            if (Keyboard.current.digit8Key.wasPressedThisFrame)
                TrySetTheme(7);

            if (Keyboard.current.digit9Key.wasPressedThisFrame)
                TrySetTheme(8);
        }

        private void TrySetTheme(int index)
        {
            if (themes == null || index >= themes.Length)
            {
                Debug.Log($"No theme assigned to key {index + 1}.");
                return;
            }

            ThemeDefinition theme = themes[index];

            if (theme == null)
            {
                Debug.Log($"Theme at key {index + 1} is null.");
                return;
            }

            if (ThemeManager.Instance == null)
            {
                Debug.LogWarning("ThemeManager instance not found.");
                return;
            }

            ThemeManager.Instance.SetTheme(theme);

            Debug.Log(
                $"Theme switched to [{index + 1}]: {theme.name}"
            );
        }
    }
}

#endif