using System;
using UnityEngine;
using Valley.Economy;
using Valley.Theming;

namespace Valley.Shop
{
    public class ThemeShopController : MonoBehaviour
    {
        public static event Action<ThemeDefinition> OnThemePurchased;
        public static event Action<ThemeDefinition> OnTemporaryThemeUnlocked;

        [Header("References")]
        [SerializeField] private CurrencyWallet wallet;

        [Header("Temporary Ad Unlock")]
        [SerializeField] private float temporaryUnlockDurationMinutes = 15f;

        private ThemeManager themeManager;
        private ThemeDefinition _lastConfirmedTheme;
        private ThemeDefinition _adTheme;
        private bool _adInFlight;

        public bool IsAdInFlight => _adInFlight;

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

            foreach (ThemeDefinition theme in themeManager.AvailableThemes)
            {
                if (theme != null &&
                    themeManager.IsPermanentlyOwned(theme))
                {
                    Debug.Log(
                        $"Theme already owned: {theme.themeName}"
                    );
                }
            }

            _lastConfirmedTheme = themeManager.CurrentTheme;
        }

        public bool IsOwned(ThemeDefinition theme)
        {
            return themeManager != null &&
                   themeManager.IsPermanentlyOwned(theme);
        }

        public bool IsTemporarilyUnlocked(ThemeDefinition theme)
        {
            return themeManager != null &&
                   themeManager.IsTemporarilyUnlocked(theme);
        }

        public bool IsUnlocked(ThemeDefinition theme)
        {
            return themeManager != null &&
                   themeManager.IsUnlocked(theme);
        }

        public TimeSpan GetTemporaryUnlockRemaining(
            ThemeDefinition theme)
        {
            return themeManager == null
                ? TimeSpan.Zero
                : themeManager.GetTemporaryUnlockRemaining(theme);
        }

        public void PreviewTheme(ThemeDefinition theme)
        {
            if (theme == null || themeManager == null)
                return;

            themeManager.SetTheme(theme);
        }

        public bool TryPurchase(ThemeDefinition theme)
        {
            if (theme == null || themeManager == null)
                return false;

            if (IsOwned(theme))
                return false;

            if (wallet == null)
            {
                wallet = CurrencyWallet.Instance;

                if (wallet == null)
                {
                    Debug.LogWarning(
                        "CurrencyWallet is not available."
                    );

                    return false;
                }
            }

            if (!wallet.TrySpend(theme.price))
            {
                Debug.LogWarning(
                    "Not enough currency to purchase theme."
                );

                return false;
            }

            themeManager.MarkThemeOwned(theme);

            _lastConfirmedTheme = theme;

            OnThemePurchased?.Invoke(theme);

            // Permanent purchase is saved to local + cloud.
            if (GameManager.Instance != null)
                GameManager.Instance.SaveGameToCloud();

            return true;
        }

        public bool TryUnlockWithAd(ThemeDefinition theme)
        {
            if (theme == null || themeManager == null)
                return false;

            if (IsOwned(theme))
                return false;

            if (IsTemporarilyUnlocked(theme))
                return false;

            if (_adInFlight)
            {
                Debug.LogWarning(
                    "A rewarded ad is already in progress."
                );

                return false;
            }

            var provider = LevelPlayAds.Instance;

            if (provider == null)
            {
                Debug.LogWarning(
                    "ThemeShop: AdManager is not available."
                );

                return false;
            }

            _adTheme = theme;
            _adInFlight = true;

            Debug.Log(
                $"Showing rewarded ad for temporary theme unlock: " +
                $"{theme.themeName}"
            );

            provider.ShowRewardedAd(
                onRewardGranted: HandleAdRewardGranted,
                onAdUnavailableOrDeclined:
                    HandleAdUnavailableOrDeclined
            );

            return true;
        }

        private void HandleAdRewardGranted()
        {
            _adInFlight = false;

            if (_adTheme == null)
            {
                Debug.LogWarning(
                    "Rewarded ad completed but no theme was assigned."
                );

                return;
            }

            ThemeDefinition unlockedTheme = _adTheme;

            _adTheme = null;

            TimeSpan duration = TimeSpan.FromMinutes(
                Mathf.Max(
                    0f,
                    temporaryUnlockDurationMinutes
                )
            );

            if (duration <= TimeSpan.Zero)
            {
                Debug.LogWarning(
                    "Temporary theme unlock duration is zero."
                );

                return;
            }

            themeManager.TemporarilyUnlockTheme(
                unlockedTheme,
                duration
            );

            _lastConfirmedTheme =
                themeManager.CurrentTheme;

            OnTemporaryThemeUnlocked?.Invoke(
                unlockedTheme
            );

            Debug.Log(
                $"Temporary theme unlocked: " +
                $"{unlockedTheme.themeName} " +
                $"for {duration.TotalMinutes:0.#} minutes."
            );
        }

        private void HandleAdUnavailableOrDeclined()
        {
            _adInFlight = false;
            _adTheme = null;

            Debug.LogWarning(
                "ThemeShop: Rewarded ad unavailable or declined."
            );
        }

        public void CloseShop()
        {
            if (themeManager == null)
                return;

            ThemeDefinition currentTheme =
                themeManager.CurrentTheme;

            if (currentTheme == null)
                return;

            if (themeManager.IsUnlocked(currentTheme))
                return;

            if (_lastConfirmedTheme != null &&
                themeManager.IsUnlocked(_lastConfirmedTheme))
            {
                themeManager.SetTheme(
                    _lastConfirmedTheme
                );

                return;
            }

            ThemeDefinition fallbackTheme =
                themeManager.GetThemeById(
                    themeManager.AvailableThemes.Count > 0 &&
                    themeManager.AvailableThemes[0] != null
                        ? themeManager.AvailableThemes[0].SaveId
                        : null
                );

            if (fallbackTheme != null &&
                themeManager.IsUnlocked(fallbackTheme))
            {
                themeManager.SetTheme(
                    fallbackTheme
                );
            }
        }
    }
}