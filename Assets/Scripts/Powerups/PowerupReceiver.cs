using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Valley.Powerups
{
    public class PowerupReceiver : MonoBehaviour
    {
        public static event Action<PowerupEffect> OnPowerupActivated;
        public static event Action<PowerupEffect, float> OnPowerupProgress;
        public static event Action<PowerupEffect> OnPowerupExpired;

        private readonly Dictionary<PowerupEffect, Coroutine> _activeTimers = new Dictionary<PowerupEffect, Coroutine>();

        private void OnEnable() => Powerup.OnPowerupCollected += HandleCollected;
        private void OnDisable() => Powerup.OnPowerupCollected -= HandleCollected;

        private void HandleCollected(PowerupEffect effect, GameObject target)
        {
            if (target != gameObject) return;

            OnPowerupActivated?.Invoke(effect);

            if (!effect.isTimed) return;

            if (_activeTimers.TryGetValue(effect, out Coroutine running))
            {
                StopCoroutine(running);
            }

            _activeTimers[effect] = StartCoroutine(TimedRoutine(effect));
        }

        private IEnumerator TimedRoutine(PowerupEffect effect)
        {
            float elapsed = 0f;

            while (elapsed < effect.duration)
            {
                elapsed += Time.deltaTime;
                float normalizedRemaining = 1f - Mathf.Clamp01(elapsed / effect.duration);
                OnPowerupProgress?.Invoke(effect, normalizedRemaining);
                yield return null;
            }

            effect.Revert(gameObject);
            _activeTimers.Remove(effect);
            OnPowerupExpired?.Invoke(effect);
        }
    }
}
