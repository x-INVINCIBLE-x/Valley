using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valley.Theming
{
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }

        public static event Action<ThemeDefinition> OnThemeChanged;
        public static event Action<ThemeDefinition> OnThemePurchased;
        public static event Action<ThemeDefinition> OnTemporaryUnlockChanged;

        [SerializeField] private ThemeDefinition initialTheme;
        [SerializeField] private ThemeDefinition[] availableThemes;

        public ThemeDefinition CurrentTheme { get; private set; }

        public IReadOnlyList<ThemeDefinition> AvailableThemes =>
            availableThemes;

        public IReadOnlyCollection<ThemeDefinition> OwnedThemes =>
            _ownedThemes;

        public ThemeDefinition TemporaryTheme =>
            _temporaryTheme;

        public ThemeDefinition PreviousTheme =>
            _previousTheme;

        public bool HasTemporaryTheme =>
            _temporaryTheme != null;

        public bool TemporaryThemeHasExpired =>
            _temporaryTheme != null &&
            DateTime.UtcNow >= _temporaryThemeExpiryUtc;

        public DateTime TemporaryThemeExpiryUtc =>
            _temporaryThemeExpiryUtc;

        private readonly HashSet<ThemeDefinition> _ownedThemes = new();

        private ThemeDefinition _temporaryTheme;
        private ThemeDefinition _previousTheme;
        private DateTime _temporaryThemeExpiryUtc;

        private bool _temporaryThemeExpired;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (initialTheme != null)
            {
                CurrentTheme = initialTheme;
                _ownedThemes.Add(initialTheme);
            }
        }

        private void Start()
        {
            if (CurrentTheme != null)
                OnThemeChanged?.Invoke(CurrentTheme);
        }

        private void Update()
        {
            CheckTemporaryThemeExpiry();
        }

        // ==================================================
        // THEME
        // ==================================================

        public void SetTheme(ThemeDefinition theme)
        {
            if (theme == null || theme == CurrentTheme)
                return;

            CurrentTheme = theme;
            OnThemeChanged?.Invoke(theme);
        }

        // ==================================================
        // OWNERSHIP
        // ==================================================

        public bool IsOwned(ThemeDefinition theme)
        {
            return IsPermanentlyOwned(theme);
        }

        public bool IsPermanentlyOwned(ThemeDefinition theme)
        {
            return theme != null &&
                   _ownedThemes.Contains(theme);
        }

        public bool IsTemporarilyUnlocked(ThemeDefinition theme)
        {
            if (theme == null)
                return false;

            if (_temporaryTheme != theme)
                return false;

            if (_temporaryThemeExpired)
                return false;

            return DateTime.UtcNow < _temporaryThemeExpiryUtc;
        }

        public bool IsUnlocked(ThemeDefinition theme)
        {
            return IsPermanentlyOwned(theme) ||
                   IsTemporarilyUnlocked(theme);
        }

        // ==================================================
        // TEMPORARY UNLOCK
        // ==================================================

        public void TemporarilyUnlockTheme(
            ThemeDefinition theme,
            TimeSpan duration)
        {
            if (theme == null || duration <= TimeSpan.Zero)
                return;

            if (IsPermanentlyOwned(theme))
                return;

            DateTime now = DateTime.UtcNow;

            bool sameTemporaryTheme =
                _temporaryTheme == theme &&
                !_temporaryThemeExpired &&
                _temporaryThemeExpiryUtc > now;

            if (!sameTemporaryTheme)
            {
                _temporaryTheme = theme;
                _previousTheme = CurrentTheme;
                _temporaryThemeExpired = false;
            }

            DateTime newExpiry =
                now + duration;

            /*
             * If the same temporary theme is rewarded again,
             * do not reduce the existing remaining duration.
             *
             * This effectively keeps the later expiry.
             */
            if (sameTemporaryTheme &&
                _temporaryThemeExpiryUtc > newExpiry)
            {
                newExpiry = _temporaryThemeExpiryUtc;
            }

            _temporaryThemeExpiryUtc = newExpiry;
            _temporaryThemeExpired = false;

            SetTheme(theme);

            OnTemporaryUnlockChanged?.Invoke(theme);
        }

        public TimeSpan GetTemporaryUnlockRemaining(
            ThemeDefinition theme)
        {
            if (theme == null)
                return TimeSpan.Zero;

            if (_temporaryTheme != theme)
                return TimeSpan.Zero;

            if (_temporaryThemeExpired)
                return TimeSpan.Zero;

            TimeSpan remaining =
                _temporaryThemeExpiryUtc -
                DateTime.UtcNow;

            return remaining > TimeSpan.Zero
                ? remaining
                : TimeSpan.Zero;
        }

        private void CheckTemporaryThemeExpiry()
        {
            if (_temporaryTheme == null)
                return;

            if (_temporaryThemeExpired)
                return;

            if (DateTime.UtcNow < _temporaryThemeExpiryUtc)
                return;

            /*
             * The temporary timer has expired.
             *
             * IMPORTANT:
             * Do not revert the theme here.
             *
             * The player is allowed to finish the current
             * game using the temporary theme.
             */
            _temporaryThemeExpired = true;

            OnTemporaryUnlockChanged?.Invoke(
                _temporaryTheme
            );
        }

        public void RevertExpiredTemporaryTheme()
        {
            if (_temporaryTheme == null)
                return;

            if (!_temporaryThemeExpired &&
                DateTime.UtcNow < _temporaryThemeExpiryUtc)
            {
                return;
            }

            ExpireTemporaryThemeAndRestore();
        }

        private void ExpireTemporaryThemeAndRestore()
        {
            ThemeDefinition expiredTheme =
                _temporaryTheme;

            ThemeDefinition themeToRestore =
                _previousTheme;

            _temporaryTheme = null;
            _previousTheme = null;
            _temporaryThemeExpiryUtc = default;
            _temporaryThemeExpired = false;

            OnTemporaryUnlockChanged?.Invoke(
                expiredTheme
            );

            if (CurrentTheme != expiredTheme)
                return;

            if (themeToRestore != null &&
                IsPermanentlyOwned(themeToRestore))
            {
                SetTheme(themeToRestore);
                return;
            }

            ThemeDefinition fallbackTheme =
                GetLastOwnedTheme();

            if (fallbackTheme != null)
                SetTheme(fallbackTheme);
        }

        // ==================================================
        // PURCHASED THEMES
        // ==================================================

        public void MarkThemeOwned(
            ThemeDefinition theme)
        {
            if (theme == null)
                return;

            if (_ownedThemes.Add(theme))
                OnThemePurchased?.Invoke(theme);
        }

        // ==================================================
        // SAVE DATA
        // ==================================================

        public string GetCurrentThemeIdForSave()
        {
            /*
             * Never save a temporary theme as the persistent
             * current theme.
             */

            if (CurrentTheme != null &&
                IsPermanentlyOwned(CurrentTheme))
            {
                return CurrentTheme.SaveId;
            }

            if (_previousTheme != null &&
                IsPermanentlyOwned(_previousTheme))
            {
                return _previousTheme.SaveId;
            }

            ThemeDefinition fallbackTheme =
                GetLastOwnedTheme();

            return fallbackTheme != null
                ? fallbackTheme.SaveId
                : string.Empty;
        }

        public string GetTemporaryThemeIdForSave()
        {
            return _temporaryTheme != null
                ? _temporaryTheme.SaveId
                : string.Empty;
        }

        public string GetPreviousThemeIdForSave()
        {
            return _previousTheme != null
                ? _previousTheme.SaveId
                : string.Empty;
        }

        public long GetTemporaryThemeExpiryTicksForSave()
        {
            return _temporaryTheme != null
                ? _temporaryThemeExpiryUtc.Ticks
                : 0;
        }

        public void RestoreTemporaryUnlock(
            string temporaryThemeId,
            long expiryTicks,
            string previousThemeId)
        {
            ClearTemporaryUnlockState();

            if (string.IsNullOrEmpty(temporaryThemeId))
                return;

            if (expiryTicks <= 0)
                return;

            ThemeDefinition temporaryTheme =
                GetThemeById(temporaryThemeId);

            if (temporaryTheme == null)
                return;

            ThemeDefinition previousTheme = null;

            if (!string.IsNullOrEmpty(previousThemeId))
            {
                previousTheme =
                    GetThemeById(previousThemeId);
            }

            DateTime expiryUtc =
                new DateTime(
                    expiryTicks,
                    DateTimeKind.Utc
                );

            /*
             * The application was closed while the temporary
             * theme was active and the timer expired while the
             * application was not running.
             *
             * Revert immediately on next launch.
             */
            if (DateTime.UtcNow >= expiryUtc)
            {
                if (previousTheme != null &&
                    IsPermanentlyOwned(previousTheme))
                {
                    SetTheme(previousTheme);
                }
                else
                {
                    ThemeDefinition fallbackTheme =
                        GetLastOwnedTheme();

                    if (fallbackTheme != null)
                        SetTheme(fallbackTheme);
                }

                OnTemporaryUnlockChanged?.Invoke(
                    temporaryTheme
                );

                return;
            }

            _temporaryTheme = temporaryTheme;
            _previousTheme = previousTheme;
            _temporaryThemeExpiryUtc = expiryUtc;
            _temporaryThemeExpired = false;

            SetTheme(temporaryTheme);

            OnTemporaryUnlockChanged?.Invoke(
                temporaryTheme
            );
        }

        public void ClearTemporaryUnlockState()
        {
            _temporaryTheme = null;
            _previousTheme = null;
            _temporaryThemeExpiryUtc = default;
            _temporaryThemeExpired = false;
        }

        // ==================================================
        // LOOKUP
        // ==================================================

        public ThemeDefinition GetThemeById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (ThemeDefinition theme in availableThemes)
            {
                if (theme != null &&
                    theme.SaveId == id)
                {
                    return theme;
                }
            }

            return null;
        }

        private ThemeDefinition GetLastOwnedTheme()
        {
            if (initialTheme != null &&
                _ownedThemes.Contains(initialTheme))
            {
                return initialTheme;
            }

            foreach (ThemeDefinition theme in availableThemes)
            {
                if (theme != null &&
                    _ownedThemes.Contains(theme))
                {
                    return theme;
                }
            }

            return null;
        }
    }
}