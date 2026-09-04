using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valley.Theming;

namespace Valley.Shop
{
    public class ThemeShopSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private GameObject lockIndicator;
        [SerializeField] private Image holdProgressImage;

        [Header("Temporary Unlock")]
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TextMeshProUGUI temporaryUnlockText;
        [SerializeField] private GameObject temporaryUnlockObject;

        [Header("Interaction")]
        [SerializeField] private float holdDurationToBuy = 0.8f;

        private static readonly Dictionary<ThemeDefinition, ThemeShopSlot> _registry =
            new Dictionary<ThemeDefinition, ThemeShopSlot>();

        private ThemeDefinition _theme;
        private ThemeShopController _controller;

        private bool _isPressed;
        private bool _purchaseTriggeredThisPress;
        private float _pressStartTime;

        public ThemeDefinition Theme => _theme;

        public static ThemeShopSlot Find(ThemeDefinition theme)
        {
            return theme != null &&
                   _registry.TryGetValue(theme, out var slot)
                ? slot
                : null;
        }

        public void Initialize(
            ThemeDefinition theme,
            ThemeShopController controller)
        {
            _theme = theme;
            _controller = controller;

            if (_theme != null)
                _registry[_theme] = this;

            if (iconImage != null)
                iconImage.sprite = theme != null ? theme.icon : null;

            if (nameText != null)
                nameText.text = theme != null ? theme.themeName : string.Empty;

            if (watchAdButton != null)
            {
                watchAdButton.onClick.RemoveListener(HandleWatchAdClicked);
                watchAdButton.onClick.AddListener(HandleWatchAdClicked);
            }

            Refresh();
        }

        private void OnEnable()
        {
            ThemeManager.OnThemeChanged += HandleThemeChanged;
            ThemeManager.OnThemePurchased += HandleThemePurchased;
            ThemeManager.OnTemporaryUnlockChanged += HandleTemporaryUnlockChanged;
            ThemeShopController.OnTemporaryThemeUnlocked += HandleTemporaryThemeUnlocked;
        }

        private void OnDisable()
        {
            ThemeManager.OnThemeChanged -= HandleThemeChanged;
            ThemeManager.OnThemePurchased -= HandleThemePurchased;
            ThemeManager.OnTemporaryUnlockChanged -= HandleTemporaryUnlockChanged;
            ThemeShopController.OnTemporaryThemeUnlocked -= HandleTemporaryThemeUnlocked;
        }

        private void OnDestroy()
        {
            if (watchAdButton != null)
                watchAdButton.onClick.RemoveListener(HandleWatchAdClicked);

            if (_theme != null &&
                _registry.TryGetValue(_theme, out var current) &&
                current == this)
            {
                _registry.Remove(_theme);
            }
        }

        private void HandleThemeChanged(ThemeDefinition theme)
        {
            Refresh();
        }

        private void HandleThemePurchased(ThemeDefinition theme)
        {
            if (theme == _theme)
                Refresh();
        }

        private void HandleTemporaryUnlockChanged(ThemeDefinition theme)
        {
            if (theme == _theme)
                Refresh();
        }

        private void HandleTemporaryThemeUnlocked(ThemeDefinition theme)
        {
            if (theme == _theme)
                Refresh();
        }

        public void Refresh()
        {
            if (_theme == null || _controller == null)
                return;

            bool permanentlyOwned = _controller.IsOwned(_theme);
            bool temporarilyUnlocked = _controller.IsTemporarilyUnlocked(_theme);
            bool unlocked = permanentlyOwned || temporarilyUnlocked;

            if (lockIndicator != null)
                lockIndicator.SetActive(!unlocked);

            if (temporaryUnlockObject != null)
                temporaryUnlockObject.SetActive(temporarilyUnlocked);

            if (priceText != null)
            {
                if (permanentlyOwned)
                {
                    priceText.text = "Owned";
                }
                else if (temporarilyUnlocked)
                {
                    TimeSpan remaining =
                        _controller.GetTemporaryUnlockRemaining(_theme);

                    priceText.text = FormatRemainingTime(remaining);
                }
                else
                {
                    priceText.text = _theme.price.ToString();
                }
            }

            if (temporaryUnlockText != null)
            {
                if (permanentlyOwned)
                {
                    temporaryUnlockText.text = string.Empty;
                }
                else if (temporarilyUnlocked)
                {
                    TimeSpan remaining =
                        _controller.GetTemporaryUnlockRemaining(_theme);

                    temporaryUnlockText.text =
                        FormatRemainingTime(remaining);
                }
                else
                {
                    temporaryUnlockText.text = "Watch Ad";
                }
            }

            if (watchAdButton != null)
            {
                watchAdButton.gameObject.SetActive(
                    !permanentlyOwned &&
                    !temporarilyUnlocked
                );

                watchAdButton.interactable =
                    !_controller.IsAdInFlight;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_theme == null || _controller == null)
                return;

            _isPressed = true;
            _purchaseTriggeredThisPress = false;
            _pressStartTime = Time.unscaledTime;

            if (holdProgressImage != null)
                holdProgressImage.fillAmount = 1f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed)
                return;

            _isPressed = false;

            if (holdProgressImage != null)
                holdProgressImage.fillAmount = 1f;

            if (_purchaseTriggeredThisPress)
                return;

            float heldDuration =
                Time.unscaledTime - _pressStartTime;

            if (heldDuration < holdDurationToBuy)
                _controller.PreviewTheme(_theme);
        }

        private void Update()
        {
            if (_theme == null || _controller == null)
                return;

            UpdateTemporaryUnlockTimer();

            if (!_isPressed || _purchaseTriggeredThisPress)
                return;

            float heldDuration =
                Time.unscaledTime - _pressStartTime;

            if (holdProgressImage != null)
            {
                holdProgressImage.fillAmount = Mathf.Clamp01(
                    (holdDurationToBuy - heldDuration) /
                    holdDurationToBuy
                );
            }

            if (heldDuration < holdDurationToBuy)
                return;

            _purchaseTriggeredThisPress = true;

            bool alreadyUnlocked = _controller.IsUnlocked(_theme);

            if (!alreadyUnlocked)
            {
                if (_controller.TryPurchase(_theme))
                    Refresh();
            }

            if (holdProgressImage != null)
                holdProgressImage.fillAmount = 1f;
        }

        private void UpdateTemporaryUnlockTimer()
        {
            if (!_controller.IsTemporarilyUnlocked(_theme))
            {
                if (temporaryUnlockObject != null)
                    temporaryUnlockObject.SetActive(false);

                return;
            }

            TimeSpan remaining =
                _controller.GetTemporaryUnlockRemaining(_theme);

            if (remaining <= TimeSpan.Zero)
            {
                if (temporaryUnlockObject != null)
                    temporaryUnlockObject.SetActive(false);

                Refresh();
                return;
            }

            if (temporaryUnlockObject != null)
                temporaryUnlockObject.SetActive(true);

            string remainingText = FormatRemainingTime(remaining);

            if (priceText != null)
                priceText.text = remainingText;

            if (temporaryUnlockText != null)
                temporaryUnlockText.text = remainingText;
        }

        private void HandleWatchAdClicked()
        {
            if (_theme == null || _controller == null)
                return;

            if (_controller.IsUnlocked(_theme))
                return;

            _controller.TryUnlockWithAd(_theme);
        }

        private static string FormatRemainingTime(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero)
                return "00:00";

            if (remaining.TotalHours >= 1)
            {
                return $"{(int)remaining.TotalHours}:{remaining.Minutes:00}:{remaining.Seconds:00}";
            }

            return $"{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }
}