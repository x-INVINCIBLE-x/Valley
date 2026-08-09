using System;
using System.Collections;
using UnityEngine;
using Valley.Combat;

namespace Valley.Revive
{
    [RequireComponent(typeof(Health))]
    public class PlayerReviveController : MonoBehaviour
    {
        public static event Action<float> OnReviveOfferStarted;
        public static event Action<float, float> OnReviveCountdownTick;
        public static event Action OnGameOver;

        [Header("Revive")]
        [Tooltip("Health granted on a successful revive.")]
        [SerializeField] private float reviveHealthAmount = 50f;
        [Tooltip("How long the revive offer stays open before it counts as declined.")]
        [SerializeField] private float offerDuration = 8f;
        [Tooltip("Must implement IRewardedAdProvider - wire in your ad SDK's integration here.")]
        [SerializeField] private MonoBehaviour adProvider;

        private Health _health;
        private Coroutine _offerRoutine;
        private bool _offerActive;

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable() => _health.OnDeath += HandleDeath;
        private void OnDisable() => _health.OnDeath -= HandleDeath;

        private void HandleDeath()
        {
            _offerActive = true;
            _offerRoutine = StartCoroutine(OfferRoutine());
        }

        private IEnumerator OfferRoutine()
        {
            OnReviveOfferStarted?.Invoke(offerDuration);

            float elapsed = 0f;
            while (elapsed < offerDuration && _offerActive)
            {
                float remaining = offerDuration - elapsed;
                OnReviveCountdownTick?.Invoke(remaining, Mathf.Clamp01(remaining / offerDuration));

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_offerActive)
            {
                EndGame();
            }
        }

        public void RequestRevive()
        {
            if (!_offerActive) return;

            GrantRevive(); // For testing purposes, you can call GrantRevive directly. In production, you would show the ad.
            var provider = adProvider as IRewardedAdProvider;
            if (provider == null)
            {
                Debug.LogWarning("PlayerReviveController: adProvider is not assigned or doesn't implement IRewardedAdProvider.");
                return;
            }
            provider.ShowRewardedAd(GrantRevive, HandleAdUnavailableOrDeclined);
        }

        public void DeclineRevive()
        {
            if (!_offerActive) return;
            EndGame();
        }

        private void HandleAdUnavailableOrDeclined()
        {
            // Offer window keeps running - the player can retry while time remains.
        }

        private void GrantRevive()
        {
            if (!_offerActive) return;

            _offerActive = false;
            if (_offerRoutine != null) StopCoroutine(_offerRoutine);

            _health.Revive(reviveHealthAmount);
        }

        private void EndGame()
        {
            _offerActive = false;
            if (_offerRoutine != null) StopCoroutine(_offerRoutine);

            OnGameOver?.Invoke();
        }
    }
}