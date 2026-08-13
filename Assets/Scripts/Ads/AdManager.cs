using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yodo1.MAS;
using Valley.Revive;
using Valley.Ads;


public class AdManager : MonoBehaviour, IRewardedAdProvider, IInterstitialAdProvider
{
    public static AdManager instance;

    public enum InterstitialResult
    {
        NotLoaded,       // No ad was ready, nothing was shown
        AlreadyShowing,  // A show request came in while one was already in progress
        ShowFailed,      // The SDK tried to show it and failed
        Closed           // The ad was shown and the user closed it
    }

    public enum RewardedResult
    {
        NotLoaded,       // No ad was ready, nothing was shown
        AlreadyShowing,  // A show request came in while one was already in progress
        ShowFailed,      // The SDK tried to show it and failed
        Earned,          // The ad played fully and the reward was granted
        Skipped          // The ad was closed early / no reward was granted
    }

    // Global events — subscribe from anywhere if you just want to react to outcomes
    // without being the one who called ShowInterstitialAds/ShowRewardedAds.
    public event Action<InterstitialResult> OnInterstitialResult;
    public event Action<RewardedResult> OnRewardedResult;
    public event Action OnInterstitialLoaded;
    public event Action OnRewardedLoaded;

    private bool rewardGrantedThisCycle = false;
    private float nextRewardLoadAllowedTime = 0f;
    private const float rewardLoadCooldown = 5f;
    private string currentScene = "";

    private bool _interstitialShowing = false;
    private bool _rewardShowing = false;
    private Action<InterstitialResult> _pendingInterstitialCallback;
    private Action<RewardedResult> _pendingRewardCallback;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        Yodo1U3dMasCallback.OnAppEnterForegroundEvent += () =>
        {
            Debug.Log(Yodo1U3dMas.TAG + ": The game has entered the foreground");
        };


        Yodo1U3dMasCallback.OnUmpCompletionEvent += (Yodo1U3dAdError error) =>
        {
            if (error == null)
                Debug.Log(Yodo1U3dMas.TAG + "OnUmpCompletionEvent success");
            else
                Debug.Log(Yodo1U3dMas.TAG + "OnUmpCompletionEvent with error " + error);
        };


        Yodo1U3dMasCallback.OnSdkInitializationEvent += (Yodo1MasSdkConfiguration config, Yodo1U3dAdError error) =>
        {
            if (config == null)
            {
                Debug.Log(Yodo1U3dMas.TAG + " SDK Init failed: " + error);
                return;
            }


            Debug.Log(Yodo1U3dMas.TAG + " SDK Init success: " + config);
            Yodo1U3dMas.SetUserIdentifier(SystemInfo.deviceUniqueIdentifier);


            InitializeBanner();
            InitializeInterstitial();
            InitializeRewarded();


            LoadInterstitialAds();
            LoadRewarded();


            Invoke(nameof(ShowBanner), 1f); // Delay banner show to ensure it's stable
            Invoke(nameof(LoadRewarded), 2f); // Optional second attempt to load rewarded
        };


        var userPrivacyConfig = new Yodo1MasUserPrivacyConfig()
            .titleBackgroundColor(Color.white)
            .titleTextColor(Color.black)
            .contentBackgroundColor(Color.white)
            .contentTextColor(Color.black)
            .buttonBackgroundColor(Color.yellow)
            .buttonTextColor(Color.white);


        var buildConfig = new Yodo1AdBuildConfig()
            .enableUserPrivacyDialog(true)
            .userPrivacyConfig(userPrivacyConfig)
            .enableATTAuthorization(true);


        Yodo1U3dMas.SetAdBuildConfig(buildConfig);
        Yodo1U3dMas.InitializeMasSdk();
    }


    private void OnEnable()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (scene.name != currentScene)
            {
                currentScene = scene.name;
                Invoke(nameof(ShowBanner), 1f);
            }
        };
    }


    public void ShowUmpForExistingUser()
    {
        var cfg = Yodo1U3dMas.GetSdkConfiguration();
        if (cfg == null) return;


        if (cfg.ConsentFlowUserGeography == Yodo1MasConsentFlowUserGeography.Gdpr)
            Yodo1U3dMas.ShowUmpForExistingUser();
    }


    private Yodo1U3dBannerAdView _banner;
    private void InitializeBanner() { }


    public void ShowBanner()
    {
        Debug.Log("[AdManager] ShowBanner() called");


        var size = Yodo1U3dBannerAdSize.Banner;
        var pos = Yodo1U3dBannerAdPosition.BannerBottom | Yodo1U3dBannerAdPosition.BannerHorizontalCenter;


        HideBanner();
        _banner = new Yodo1U3dBannerAdView(size, pos);
        _banner.OnAdLoadedEvent += (_) => Debug.Log("Banner loaded");
        _banner.OnAdFailedToLoadEvent += (_, e) => Debug.LogError(" Banner load failed: " + e);
        _banner.OnAdOpenedEvent += (_) => Debug.Log("Banner opened");
        _banner.OnAdClosedEvent += (_) => Debug.Log("Banner closed");
        _banner.OnAdPayRevenueEvent += (_, val) =>
        {
            if (val != null)
                Debug.Log("Banner revenue: " + val.Revenue);
        };


        _banner.LoadAd();
    }


    public void HideBanner()
    {
        if (_banner == null) return;
        _banner.Hide();
        _banner.Destroy();
        _banner = null;
    }


    private void InitializeInterstitial()
    {
        var ad = Yodo1U3dInterstitialAd.GetInstance();

        ad.OnAdLoadedEvent += (_) => OnInterstitialLoaded?.Invoke();

        ad.OnAdLoadFailedEvent += (_, err) => Invoke(nameof(LoadInterstitialAds), 5f);

        ad.OnAdOpenFailedEvent += (_, err) =>
        {
            FinishInterstitial(InterstitialResult.ShowFailed);
        };

        ad.OnAdClosedEvent += (_) =>
        {
            FinishInterstitial(InterstitialResult.Closed);
            LoadInterstitialAds();
        };
    }


    public void LoadInterstitialAds() => Yodo1U3dInterstitialAd.GetInstance().LoadAd();
    public bool IsInterstitialLoaded() => Yodo1U3dInterstitialAd.GetInstance().IsLoaded();

    public bool ShowInterstitialAds(string placement = null, Action<InterstitialResult> onResult = null)
    {
        if (_interstitialShowing)
        {
            onResult?.Invoke(InterstitialResult.AlreadyShowing);
            return false;
        }

        if (!IsInterstitialLoaded())
        {
            onResult?.Invoke(InterstitialResult.NotLoaded);
            return false;
        }

        _interstitialShowing = true;
        _pendingInterstitialCallback = onResult;

        if (string.IsNullOrEmpty(placement))
            Yodo1U3dInterstitialAd.GetInstance().ShowAd();
        else
            Yodo1U3dInterstitialAd.GetInstance().ShowAd(placement);

        return true;
    }

    private void FinishInterstitial(InterstitialResult result)
    {
        _interstitialShowing = false;

        var callback = _pendingInterstitialCallback;
        _pendingInterstitialCallback = null;

        callback?.Invoke(result);
        OnInterstitialResult?.Invoke(result);
    }


    private void InitializeRewarded()
    {
        var ad = Yodo1U3dRewardAd.GetInstance();

        ad.OnAdLoadedEvent += (_) => OnRewardedLoaded?.Invoke();

        ad.OnAdEarnedEvent += (_) =>
        {
            rewardGrantedThisCycle = true;
        };

        // NOTE: OnAdShowFailedEvent is assumed to exist on Yodo1U3dRewardAd by
        // analogy with the banner ad callbacks. Verify against your SDK version
        // if this doesn't compile.
        ad.OnAdOpenFailedEvent += (_, err) =>
        {
            FinishRewarded(RewardedResult.ShowFailed);
        };

        ad.OnAdClosedEvent += (_) =>
        {
            var result = rewardGrantedThisCycle ? RewardedResult.Earned : RewardedResult.Skipped;
            rewardGrantedThisCycle = false;
            FinishRewarded(result);
            LoadRewarded();
        };


        ad.OnAdLoadFailedEvent += (_, err) => Invoke(nameof(LoadRewarded), 5f);
    }


    public void LoadRewarded()
    {
        if (Time.time < nextRewardLoadAllowedTime) return;
        Debug.Log("[AdManager] LoadRewarded() called");
        Yodo1U3dRewardAd.GetInstance().LoadAd();
        nextRewardLoadAllowedTime = Time.time + rewardLoadCooldown;
    }


    public bool IsRewardedLoaded() => Yodo1U3dRewardAd.GetInstance().IsLoaded();

    /// <summary>
    /// Shows a rewarded ad. Returns true if the show attempt was actually
    /// started, false otherwise — in the false case, onResult is invoked
    /// immediately with the reason. The final outcome (Earned / Skipped /
    /// ShowFailed) always arrives via onResult and/or the OnRewardedResult
    /// event once the ad finishes, so this is the call to gate any reward
    /// logic on — never assume the reward was granted just because Show
    /// returned true.
    /// </summary>
    public bool ShowRewardedAds(string placement = null, Action<RewardedResult> onResult = null)
    {
        if (_rewardShowing)
        {
            onResult?.Invoke(RewardedResult.AlreadyShowing);
            return false;
        }

        if (!IsRewardedLoaded())
        {
            onResult?.Invoke(RewardedResult.NotLoaded);
            return false;
        }

        _rewardShowing = true;
        _pendingRewardCallback = onResult;

        if (string.IsNullOrEmpty(placement))
            Yodo1U3dRewardAd.GetInstance().ShowAd();
        else
            Yodo1U3dRewardAd.GetInstance().ShowAd(placement);

        return true;
    }

    private void FinishRewarded(RewardedResult result)
    {
        _rewardShowing = false;

        var callback = _pendingRewardCallback;
        _pendingRewardCallback = null;

        callback?.Invoke(result);
        OnRewardedResult?.Invoke(result);
    }

    // ---- IRewardedAdProvider ----
    // Adapts the result-based ShowRewardedAds API to the simpler two-callback
    // contract PlayerReviveController expects. ShowRewardedAds always invokes
    // its callback exactly once (either synchronously, if nothing was loaded
    // or shown, or once the ad closes), so this fires exactly one of the two
    // callbacks below, exactly once.
    public void ShowRewardedAd(Action onRewardGranted, Action onAdUnavailableOrDeclined)
    {
        ShowRewardedAds(null, result =>
        {
            if (result == RewardedResult.Earned)
                onRewardGranted?.Invoke();
            else
                onAdUnavailableOrDeclined?.Invoke();
        });
    }

    // ---- IInterstitialAdProvider ----
    public void ShowInterstitialAd()
    {
        ShowInterstitialAds();
    }

    private void OnDestroy()
    {
        HideBanner();
    }
}