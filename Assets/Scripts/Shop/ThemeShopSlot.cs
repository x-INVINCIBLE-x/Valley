using System.Collections.Generic;
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
        [SerializeField] private Text nameText;
        [SerializeField] private Text priceText;
        [SerializeField] private GameObject lockIndicator;
        [Tooltip("Optional - fills 0 to 1 while held, to show purchase-hold progress.")]
        [SerializeField] private Image holdProgressImage;

        [Header("Interaction")]
        [Tooltip("A tap shorter than this previews the theme. Holding past this attempts a purchase.")]
        [SerializeField] private float holdDurationToBuy = 0.8f;

        private static readonly Dictionary<ThemeDefinition, ThemeShopSlot> _registry = new Dictionary<ThemeDefinition, ThemeShopSlot>();

        private ThemeDefinition _theme;
        private ThemeShopController _controller;

        private bool _isPressed;
        private bool _purchaseTriggeredThisPress;
        private float _pressStartTime;

        public ThemeDefinition Theme => _theme;

        public static ThemeShopSlot Find(ThemeDefinition theme)
        {
            return theme != null && _registry.TryGetValue(theme, out var slot) ? slot : null;
        }

        public void Initialize(ThemeDefinition theme, ThemeShopController controller)
        {
            _theme = theme;
            _controller = controller;
            _registry[theme] = this;

            if (iconImage != null) iconImage.sprite = theme.icon;
            if (nameText != null) nameText.text = theme.themeName;

            Refresh();
        }

        private void OnDestroy()
        {
            if (_theme != null && _registry.TryGetValue(_theme, out var current) && current == this)
            {
                _registry.Remove(_theme);
            }
        }

        public void Refresh()
        {
            bool owned = _controller.IsOwned(_theme);

            if (lockIndicator != null) lockIndicator.SetActive(!owned);
            if (priceText != null) priceText.text = owned ? "Owned" : _theme.price.ToString();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            _purchaseTriggeredThisPress = false;
            _pressStartTime = Time.unscaledTime;

            if (holdProgressImage != null) holdProgressImage.fillAmount = 1f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed) return;
            _isPressed = false;

            if (holdProgressImage != null) holdProgressImage.fillAmount = 1f;

            if (_purchaseTriggeredThisPress) return;

            float heldDuration = Time.unscaledTime - _pressStartTime;
            if (heldDuration < holdDurationToBuy)
            {
                _controller.PreviewTheme(_theme);
            }
        }

        private void Update()
        {
            if (!_isPressed || _purchaseTriggeredThisPress) return;

            float heldDuration = Time.unscaledTime - _pressStartTime;

            if (holdProgressImage != null)
            {
                holdProgressImage.fillAmount = Mathf.Clamp01(
                    (holdDurationToBuy - heldDuration) / holdDurationToBuy);
            }

            if (heldDuration < holdDurationToBuy) return;

            _purchaseTriggeredThisPress = true;
            if (_controller.TryPurchase(_theme)) Refresh();

            if (holdProgressImage != null) holdProgressImage.fillAmount = 1f;
        }
    }
}