using System;
using UnityEngine;
using Valley.Aiming;
using Valley.Combat;

namespace Valley.Player
{
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        public static event Action<float, float> OnPlayerHealthChanged;
        public static event Action OnPlayerDamaged;
        public static event Action OnPlayerDied;

        private Health _health;

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable()
        {
            _health.OnHealthUpdated += HandleHealthChanged;
            _health.OnDamaged += HandleDamaged;
            _health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _health.OnHealthUpdated -= HandleHealthChanged;
            _health.OnDamaged -= HandleDamaged;
            _health.OnDeath -= HandleDeath;
        }

        private void HandleHealthChanged(float current, float max) => OnPlayerHealthChanged?.Invoke(current, max);

        private void HandleDamaged(float amount, GameObject source) => OnPlayerDamaged?.Invoke();

        private void HandleDeath()
        {
            OnPlayerDied?.Invoke();

            var aim = GetComponent<AimInputController>();
            if (aim != null) aim.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }
    }
}