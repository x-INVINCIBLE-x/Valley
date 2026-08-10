using UnityEngine;
using Valley.Revive;

namespace Valley.Ads
{
    public class InterstitialAdScheduler : MonoBehaviour
    {
        [Header("Frequency")]
        [Tooltip("Show an interstitial ad after every this many completed plays (a play ends via PlayerReviveController.OnGameOver - successful revives don't count as a play ending).")]
        [SerializeField] private int playsPerAd = 3;
        [Tooltip("Must implement IInterstitialAdProvider - wire in your ad SDK's integration here.")]
        [SerializeField] private MonoBehaviour adProvider;

        private int _playsSinceLastAd;

        private void OnEnable() => PlayerReviveController.OnGameOver += HandlePlayCompleted;
        private void OnDisable() => PlayerReviveController.OnGameOver -= HandlePlayCompleted;

        private void HandlePlayCompleted()
        {
            _playsSinceLastAd++;

            if (_playsSinceLastAd < playsPerAd) return;

            _playsSinceLastAd = 0;
            ShowAd();
        }

        private void ShowAd()
        {
            var provider = adProvider as IInterstitialAdProvider;
            if (provider == null)
            {
                Debug.LogWarning("InterstitialAdScheduler: adProvider is not assigned or doesn't implement IInterstitialAdProvider.");
                return;
            }

            provider.ShowInterstitialAd();
        }
    }
}