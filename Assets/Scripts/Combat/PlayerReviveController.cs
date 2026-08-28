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
        private bool _adInFlight;

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
                // Freeze the countdown while an ad is loading/playing so a slow
                // ad can't run the offer window out from under the player.
                if (!_adInFlight)
                {
                    float remaining = offerDuration - elapsed;
                    OnReviveCountdownTick?.Invoke(remaining, Mathf.Clamp01(remaining / offerDuration));
                    elapsed += Time.unscaledDeltaTime;
                }

                yield return null;
            }

            if (_offerActive)
            {
                EndGame();
            }
        }

        public void RequestRevive()
        {
            if (!_offerActive || _adInFlight) return;

            //var provider = adProvider as IRewardedAdProvider;
            var provider = LevelPlayAds.Instance;

            if (provider == null)
            {
                Debug.LogWarning("PlayerReviveController: adProvider is not assigned or doesn't implement IRewardedAdProvider.");
                return;
            }

            _adInFlight = true;
            provider.ShowRewardedAd(
                onRewardGranted: () =>
                {
                    _adInFlight = false;
                    GrantRevive();
                },
                onAdUnavailableOrDeclined: () =>
                {
                    _adInFlight = false;
                    HandleAdUnavailableOrDeclined();
                });
        }

        public void DeclineRevive()
        {
            if (!_offerActive) return;
            EndGame();
        }

        private void HandleAdUnavailableOrDeclined()
        {
            Debug.Log("Failed");
            // Offer window keeps running - the player can retry while time remains.
        }

        private void GrantRevive()
        {
            Debug.Log("revive");
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