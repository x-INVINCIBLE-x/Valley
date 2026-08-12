using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using System;
using UnityEngine;
using Valley.Combat;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private MMProgressBar[] healthBars;
    [SerializeField] private MMF_Player[] healFeedbacks;
    [SerializeField] private MMF_Player[] damageFeedbacks;

    private void OnEnable()
    {
        health.OnHealthUpdated += HandleHealthUpdate;
        health.OnDamaged += HandleDamage;
        health.OnHeal += HandleHeal;

        Initialize();
    }

    private void OnDisable()
    {
        health.OnHealthUpdated -= HandleHealthUpdate;
        health.OnDamaged -= HandleDamage;
        health.OnHeal -= HandleHeal;
    }

    private void Initialize()
    {
        foreach (var healthBar in healthBars)
        {
            healthBar.Initialization();
            healthBar.UpdateBar(health.Current, 0f, health.MaxHealth);
        }
    }

    private void HandleHealthUpdate(float current, float maxHealth)
    {
        foreach (var healthBar in healthBars)
        {
            healthBar.UpdateBar(current, 0f, maxHealth);
        }
    }

    private void HandleHeal(float amount)
    {
        foreach (var healFeedback in healFeedbacks)
        {
            healFeedback.PlayFeedbacks();
        }
    }

    private void HandleDamage(float amount, GameObject source)
    {
        foreach (var damageFeedback in damageFeedbacks)
        {
            damageFeedback.PlayFeedbacks();
        }
    }
}