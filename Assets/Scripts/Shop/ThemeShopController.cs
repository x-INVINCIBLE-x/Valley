using System;
using System.Collections.Generic;
using UnityEngine;
using Valley.Theming;
using Valley.Economy;

namespace Valley.Shop
{
    public class ThemeShopController : MonoBehaviour
    {
        public static event Action<ThemeDefinition> OnThemePurchased;

        [SerializeField] private CurrencyWallet wallet;

        private readonly HashSet<ThemeDefinition> _ownedThemes = new HashSet<ThemeDefinition>();
        private ThemeDefinition _lastConfirmedTheme;
        private ThemeManager themeManager;

        private void Start()
        {
            themeManager = ThemeManager.Instance;
            wallet = CurrencyWallet.Instance;

            if (themeManager == null)
            {
                gameObject.SetActive(false);
                Debug.LogWarning("ThemeManager instance not found. ThemeShopController will be disabled.");
            }

            _lastConfirmedTheme = themeManager.CurrentTheme;
            if (_lastConfirmedTheme != null) _ownedThemes.Add(_lastConfirmedTheme);
        }

        public bool IsOwned(ThemeDefinition theme) => theme != null && _ownedThemes.Contains(theme);

        public void PreviewTheme(ThemeDefinition theme)
        {
            if (theme == null) return;
            themeManager.SetTheme(theme);
        }

        public bool TryPurchase(ThemeDefinition theme)
        {
            if (theme == null || IsOwned(theme))
            {
                Debug.LogWarning("Theme is null or already owned.");
                return false;
            }

            if (!wallet.TrySpend(theme.price))
            {
                Debug.LogWarning("Not enough currency to purchase theme.");
                return false;
            }

            _ownedThemes.Add(theme);
            _lastConfirmedTheme = theme;
            OnThemePurchased?.Invoke(theme);
            return true;
        }

        public void CloseShop()
        {
            if (!IsOwned(themeManager.CurrentTheme))
            {
                themeManager.SetTheme(_lastConfirmedTheme);
            }
        }
    }
}