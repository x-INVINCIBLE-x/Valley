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

        [Tooltip("Maximum number of revives allowed during this run.")]
        [SerializeField] private int maxRevives = 2;

        [Tooltip("Must implement IRewardedAdProvider - wire in your ad SDK's integration here.")]
        [SerializeField] private MonoBehaviour adProvider;

        private Health _health;
        private Coroutine _offerRoutine;
        private bool _offerActive;
        private bool _adInFlight;

        private int _reviveCount;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _health.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            // No revive offer if the player has already used all revives.
            if (_reviveCount >= maxRevives)
            {
                EndGame();
                return;
            }

            _offerActive = true;
            _offerRoutine = StartCoroutine(OfferRoutine());
        }

        private IEnumerator OfferRoutine()
        {
            OnReviveOfferStarted?.Invoke(offerDuration);

            float elapsed = 0f;

            while (elapsed < offerDuration && _offerActive)
            {
                // Freeze the countdown while an ad is loading/playing.
                if (!_adInFlight)
                {
                    float remaining = offerDuration - elapsed;

                    OnReviveCountdownTick?.Invoke(
                        remaining,
                        Mathf.Clamp01(remaining / offerDuration)
                    );

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
            if (!_offerActive || _adInFlight)
                return;

            // Check revive limit before showing the ad.
            if (_reviveCount >= maxRevives)
            {
                EndGame();
                return;
            }

            var provider = LevelPlayAds.Instance;

            if (provider == null)
            {
                Debug.LogWarning(
                    "PlayerReviveController: LevelPlayAds instance is not available."
                );
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
            if (!_offerActive)
                return;

            EndGame();
        }

        private void HandleAdUnavailableOrDeclined()
        {
            Debug.Log("Rewarded ad unavailable or declined.");

            // Offer window keeps running.
            // Player can retry while time remains.
        }

        private void GrantRevive()
        {
            if (!_offerActive)
                return;

            // Increment only after the reward has actually been granted.
            _reviveCount++;

            Debug.Log($"Revive used: {_reviveCount}/{maxRevives}");

            _offerActive = false;

            if (_offerRoutine != null)
            {
                StopCoroutine(_offerRoutine);
                _offerRoutine = null;
            }

            _health.Revive(reviveHealthAmount);
        }

        private void EndGame()
        {
            _offerActive = false;

            if (_offerRoutine != null)
            {
                StopCoroutine(_offerRoutine);
                _offerRoutine = null;
            }

            OnGameOver?.Invoke();
        }

        /// <summary>
        /// Number of successful revives used during this run.
        /// </summary>
        public int RevivesUsed => _reviveCount;

        /// <summary>
        /// Number of revives remaining during this run.
        /// </summary>
        public int RevivesRemaining => Mathf.Max(0, maxRevives - _reviveCount);
    }
}