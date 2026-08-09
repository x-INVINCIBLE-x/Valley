using System;
using UnityEngine;
using Valley.Combat;
using Valley.Aiming;

namespace Valley.Player
{
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        public static event Action<float, float> OnPlayerHealthChanged;
        public static event Action<float> OnPlayerHealed;
        public static event Action OnPlayerDamaged;
        public static event Action OnPlayerDied;
        public static event Action OnPlayerRevived;

        private Health _health;

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable()
        {
            _health.OnHealthUpdated += HandleHealthUpdated;
            _health.OnHeal += HandleHeal;
            _health.OnDamaged += HandleDamaged;
            _health.OnDeath += HandleDeath;
            _health.OnRevived += HandleRevived;
        }

        private void OnDisable()
        {
            _health.OnHealthUpdated -= HandleHealthUpdated;
            _health.OnHeal -= HandleHeal;
            _health.OnDamaged -= HandleDamaged;
            _health.OnDeath -= HandleDeath;
            _health.OnRevived -= HandleRevived;
        }

        private void HandleHealthUpdated(float current, float max) => OnPlayerHealthChanged?.Invoke(current, max);

        private void HandleHeal(float amount) => OnPlayerHealed?.Invoke(amount);

        private void HandleDamaged(float amount, GameObject source) => OnPlayerDamaged?.Invoke();

        private void HandleDeath()
        {
            OnPlayerDied?.Invoke();

            var aim = GetComponent<InputController>();
            if (aim != null) aim.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        private void HandleRevived()
        {
            var aim = GetComponent<InputController>();
            if (aim != null) aim.enabled = true;

            OnPlayerRevived?.Invoke();
        }
    }
}