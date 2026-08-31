using System;
using UnityEngine;
using Valley.Theming;
using Valley.Economy;

namespace Valley.Shop
{
    public class ThemeShopController : MonoBehaviour
    {
        public static event Action<ThemeDefinition> OnThemePurchased;

        [SerializeField] private CurrencyWallet wallet;

        private ThemeManager themeManager;
        private ThemeDefinition _lastConfirmedTheme;

        private void Start()
        {
            themeManager = ThemeManager.Instance;
            wallet = CurrencyWallet.Instance;

            if (themeManager == null)
            {
                Debug.LogWarning("ThemeManager instance not found.");
                gameObject.SetActive(false);
                return;
            }

            // Query ThemeManager for already purchased themes.
            // This makes the shop reflect saved ownership every time
            // the shop scene is opened.
            foreach (ThemeDefinition theme in themeManager.AvailableThemes)
            {
                if (theme != null && themeManager.IsOwned(theme))
                    Debug.Log($"Theme already owned: {theme.themeName}");
            }

            _lastConfirmedTheme = themeManager.CurrentTheme;
        }

        public bool IsOwned(ThemeDefinition theme)
        {
            return themeManager != null && themeManager.IsOwned(theme);
        }

        public void PreviewTheme(ThemeDefinition theme)
        {
            if (theme == null)
                return;

            themeManager.SetTheme(theme);
        }

        public bool TryPurchase(ThemeDefinition theme)
        {
            if (theme == null || IsOwned(theme))
                return false;

            if (wallet == null || !wallet.TrySpend(theme.price))
            {
                Debug.LogWarning("Not enough currency to purchase theme.");
                return false;
            }

            themeManager.MarkThemeOwned(theme);

            _lastConfirmedTheme = theme;

            OnThemePurchased?.Invoke(theme);

            // Save the newly purchased theme.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGameToCloud();
            }

            return true;
        }

        public void CloseShop()
        {
            if (!IsOwned(themeManager.CurrentTheme))
                themeManager.SetTheme(_lastConfirmedTheme);
        }
    }
}