using System;
using UnityEngine;
using Valley.Combat;

namespace Valley.Combat
{
    public class Health : MonoBehaviour, IDamageable
    {
        // <Current, MaxHealth>
        public event Action<float, float> OnHealthUpdated;
        public event Action<float, GameObject> OnDamaged;
        public event Action<float> OnHeal;

        public event Action OnDeath;

        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath = false;

        public float MaxHealth => maxHealth;
        [field: SerializeField] public float Current { get; private set; }
        public bool IsDead { get; private set; }

        private void Awake()
        {
            Current = maxHealth;
            OnHealthUpdated?.Invoke(Current, maxHealth);
        }

        public void ApplyDamage(float amount, GameObject source)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            OnDamaged?.Invoke(amount, source);
            OnHealthUpdated?.Invoke(Current, maxHealth);

            if (Current <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Min(maxHealth, Current + amount);
            OnHeal?.Invoke(amount);
            OnHealthUpdated?.Invoke(Current, maxHealth);
        }

        private void Die()
        {
            IsDead = true;
            OnDeath?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }
}