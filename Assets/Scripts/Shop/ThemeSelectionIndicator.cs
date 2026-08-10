using UnityEngine;
using Valley.Theming;

namespace Valley.Shop
{
    public class ThemeSelectionIndicator : MonoBehaviour
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            ThemeManager.OnThemeChanged += HandleThemeChanged;
        }

        private void OnDestroy()
        {
            ThemeManager.OnThemeChanged -= HandleThemeChanged;
        }

        private void Start()
        {
            if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
            {
                HandleThemeChanged(ThemeManager.Instance.CurrentTheme);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void HandleThemeChanged(ThemeDefinition theme)
        {
            ThemeShopSlot slot = ThemeShopSlot.Find(theme);

            if (slot == null)
            {
                gameObject.SetActive(false);
                return;
            }

            transform.SetParent(slot.transform, false);

            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                transform.localPosition = Vector3.zero;
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }
    }
}