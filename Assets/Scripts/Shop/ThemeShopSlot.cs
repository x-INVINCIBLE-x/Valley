using MoreMountains.Feedbacks;
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
        [Tooltip("Optional - fills 0 to 1 while held, to show purchase-hold progress.")]
        [SerializeField] private Image holdProgressImage;

        [Header("Interaction")]
        [Tooltip("A tap shorter than this previews the theme. Holding past this attempts a purchase.")]
        [SerializeField] private float holdDurationToBuy = 0.8f;

        private ThemeDefinition _theme;
        private ThemeShopController _controller;

        private bool _isPressed;
        private bool _purchaseTriggeredThisPress;
        private float _pressStartTime;

        public void Initialize(ThemeDefinition theme, ThemeShopController controller)
        {
            _theme = theme;
            _controller = controller;

            if (iconImage != null) iconImage.sprite = theme.icon;
            if (nameText != null) nameText.text = theme.themeName;

            Refresh();
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
            Debug.Log("Purchase triggered for theme: " + _theme.themeName);
            _purchaseTriggeredThisPress = true;

            bool status = _controller.TryPurchase(_theme);
            if (status) Refresh();

            Debug.Log("Purchase " + (status ? "successful" : "failed") + " for theme: " + _theme.themeName);

            if (holdProgressImage != null) holdProgressImage.fillAmount = 0f;
        }
    }
}