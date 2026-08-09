using System.Collections.Generic;
using UnityEngine;
using Valley.Theming;

namespace Valley.Shop
{
    public class ThemeShopUI : MonoBehaviour
    {
        [SerializeField] private ThemeShopController controller;
        [SerializeField] private ThemeShopSlot slotTemplate;
        [SerializeField] private Transform slotContainer;

        private readonly List<ThemeShopSlot> _spawnedSlots = new List<ThemeShopSlot>();

        private void Start()
        {
            slotTemplate.gameObject.SetActive(false);

            if (ThemeManager.Instance == null) return;

            foreach (var theme in ThemeManager.Instance.AvailableThemes)
            {
                if (theme == null) continue;

                var slot = Instantiate(slotTemplate, slotContainer);
                slot.gameObject.SetActive(true);
                slot.Initialize(theme, controller);
                _spawnedSlots.Add(slot);
            }
        }

        private void OnEnable() => ThemeShopController.OnThemePurchased += HandleThemePurchased;

        private void OnDisable()
        {
            ThemeShopController.OnThemePurchased -= HandleThemePurchased;
            controller.CloseShop();
        }

        private void HandleThemePurchased(ThemeDefinition theme)
        {
            foreach (var slot in _spawnedSlots) slot.Refresh();
        }
    }
}