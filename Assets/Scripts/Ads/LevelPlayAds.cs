using System;
using Unity.Services.LevelPlay;
using UnityEngine;
using Valley.Ads;
using Valley.Revive;
using Yodo1.MAS;

public class LevelPlayAds : MonoBehaviour, IInterstitialAdProvider, IRewardedAdProvider
{
    [Header("App Key")]
    [SerializeField] private string androidAppKey;
    [SerializeField] private string iosAppKey;

    [Header("Banner Ad Unit ID")]
    [SerializeField] private string androidBannerAdUnitId;
    [SerializeField] private string iosBannerAdUnitId;

    [Header("Interstitial Ad Unit ID")]
    [SerializeField] private string androidInterstitialAdUnitId;
    [SerializeField] private string iosinterstitialAdUnitId;

    [Header("Rewarded Ad Unit ID")]
    [SerializeField] private string androidRewardedAdUnitId;
    [SerializeField] private string iosRewardedAdUnitId;

    #region keys
    private string appKey
    {
        get
        {
#if UNITY_ANDROID
            return androidAppKey;
#else
        return "";
#endif
        }
    }

    private string bannerAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidBannerAdUnitId;
#else
        return "";
#endif
        }
    }

    private string interstitialAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidInterstitialAdUnitId;
#else
        return "";
#endif
        }
    }

    private string rewardedAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidRewardedAdUnitId;
#else
        return "";
#endif
        }
    }
    #endregion

    private LevelPlayBannerAd bannerAd;
    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedAd;

    private Action onRewardGranted;
    private Action onAdUnavailableOrDeclined;

    private bool rewardGranted;

    public void Start()
    {
        LevelPlay.ValidateIntegration();

        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

        LevelPlay.Init(appKey);
    }

    private void SdkInitializationCompletedEvent(LevelPlayConfiguration configuration)
    {
        CreateBannerAd();
        CreateInterstitialAd();
        CreateRewardedAd();
    }

    private void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        throw new NotImplementedException();
    }

    #region BannerAd
    private void CreateBannerAd()
    {
        var addConfig = new LevelPlayBannerAd.Config.Builder()
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .Build();

        bannerAd = new LevelPlayBannerAd(bannerAdUnitId, addConfig);

        bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
        bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
        bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
        bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
        bannerAd.OnAdClicked += BannerOnAdClickedEvent;
        bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
        bannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
        bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;
    }

    void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdLoadFailedEvent(LevelPlayAdError ironSourceError) { }
    void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
    void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo) { }
    void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo) { }

    public void ShowBanner()
    {
        bannerAd.LoadAd();
    }

    public void DestroyBanner()
    {
        bannerAd.DestroyAd();
    }
    #endregion

    #region InterstitialAd
    public void CreateInterstitialAd()
    {
        interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitId);

        interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
        interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
        interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
        interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
        interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
        interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
        interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;

        LoadInterstitialAd();
    }

    public void LoadInterstitialAd()
    {
        interstitialAd.LoadAd();
    }

    void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
    void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error) 
    {
        LoadInterstitialAd();
    }
    void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
    void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
    void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        LoadInterstitialAd();
    }
    void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) { }


    public void ShowInterstitialAd()
    {
        if (interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
        }
    }
    #endregion

    #region RewardedAD
    private void CreateRewardedAd()
    {
        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

        rewardedAd.OnAdLoaded += RewardedOnAdLoadedEvent;
        rewardedAd.OnAdLoadFailed += RewardedOnAdLoadFailedEvent;
        rewardedAd.OnAdDisplayed += RewardedOnAdDisplayedEvent;
        rewardedAd.OnAdDisplayFailed += RewardedOnAdDisplayFailedEvent;
        rewardedAd.OnAdRewarded += RewardedOnAdRewardedEvent;
        rewardedAd.OnAdClosed += RewardedOnAdClosedEvent;
        // Optional
        rewardedAd.OnAdClicked += RewardedOnAdClickedEvent;
        rewardedAd.OnAdInfoChanged += RewardedOnAdInfoChangedEvent;

        LoadRewardedAd();
    }

    void RewardedOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }

    void RewardedOnAdLoadFailedEvent(LevelPlayAdError error)
    {
        LoadRewardedAd();
    }

    void RewardedOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
        rewardGranted = false;
    }

    void RewardedOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        // The ad was not successfully displayed.
        InvokeUnavailableCallback();

        // Prepare another ad
        LoadRewardedAd();
    }

    void RewardedOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward adReward)
    {
        rewardGranted = true;

        InvokeRewardCallback();
    }

    void RewardedOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
        if (!rewardGranted)
        {
            InvokeUnavailableCallback();
        }

        // Prepare the next ad
        LoadRewardedAd();

        // Reset state
        rewardGranted = false;
    }

    void RewardedOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
    void RewardedOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) { }

    private void LoadRewardedAd()
    {
        rewardedAd.LoadAd();
    }

    public void ShowRewardedAd(Action onRewardGranted, Action onAdUnavailableOrDeclined)
    {
        if (rewardedAd == null || !rewardedAd.IsAdReady())
        {
            Debug.Log("Rewarded ad is not ready.");

            onAdUnavailableOrDeclined?.Invoke();
            return;
        }

        this.onRewardGranted = onRewardGranted;
        this.onAdUnavailableOrDeclined = onAdUnavailableOrDeclined;

        rewardGranted = false;

        rewardedAd.ShowAd();
    }

    private void InvokeRewardCallback()
    {
        onRewardGranted?.Invoke();

        // Clear callbacks so they cannot be called again
        ClearCallbacks();
    }

    private void InvokeUnavailableCallback()
    {
        onAdUnavailableOrDeclined?.Invoke();

        // Clear callbacks so they cannot be called again
        ClearCallbacks();
    }

    private void ClearCallbacks()
    {
        onRewardGranted = null;
        onAdUnavailableOrDeclined = null;
    }
    #endregion
}
