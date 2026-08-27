using System;
using UnityEngine;
using Valley.Economy;
using Valley.Revive;

public class DailyReward
{
    private IRewardedAdProvider _rewardedAdProvider;
    private readonly CurrencyWallet _wallet;
    private readonly int _rewardAmount;
    private readonly int _maxRewardsPerDay;

    private bool _adInFlight;

    private int _rewardsClaimedToday;
    private string _lastRewardDate;

    public event Action OnRewardSuccess;
    public event Action<string> OnRewardFailure;
    public event Action<int, int> OnProgressChanged;

    public int RewardsClaimedToday => _rewardsClaimedToday;
    public int MaxRewards => _maxRewardsPerDay;

    public int RemainingAttempts =>
        Mathf.Max(0, _maxRewardsPerDay - _rewardsClaimedToday);

    public DailyReward(
        IRewardedAdProvider adProvider,
        CurrencyWallet wallet,
        int rewardAmount,
        int maxRewardsPerDay)
    {
        _rewardedAdProvider = adProvider;
        _wallet = wallet;
        _rewardAmount = rewardAmount;
        _maxRewardsPerDay = Mathf.Max(1, maxRewardsPerDay);

        LoadDailyState();
    }

    public void HandleReward()
    {
        ResetIfNewDay();

        if (_adInFlight)
            return;

        if (RemainingAttempts <= 0)
        {
            OnRewardFailure?.Invoke("Daily limit reached.");
            return;
        }

        var provider = _rewardedAdProvider;

        if (provider == null)
        {
            Debug.LogWarning("DailyReward: AdManager is not available.");

            OnRewardFailure?.Invoke("Ad unavailable.");
            return;
        }

        _adInFlight = true;
        Debug.Log("Rewared");
        provider.ShowRewardedAd(
            onRewardGranted: () =>
            {
                _adInFlight = false;
                GrantReward();
            },

            onAdUnavailableOrDeclined: () =>
            {
                _adInFlight = false;
                HandleAdUnavailableOrDeclined();
            });
    }

    private void GrantReward()
    {
        if (RemainingAttempts <= 0)
            return;
        Debug.Log("Reward Granted");
        _rewardsClaimedToday++;

        SaveDailyState();

        _wallet.Add(_rewardAmount);

        OnProgressChanged?.Invoke(
            _rewardsClaimedToday,
            _maxRewardsPerDay);

        OnRewardSuccess?.Invoke();
    }

    private void HandleAdUnavailableOrDeclined()
    {
        OnRewardFailure?.Invoke("Ad unavailable.");
    }

    private void ResetIfNewDay()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (_lastRewardDate == today)
            return;

        _lastRewardDate = today;
        _rewardsClaimedToday = 0;

        SaveDailyState();

        OnProgressChanged?.Invoke(
            _rewardsClaimedToday,
            _maxRewardsPerDay);
    }

    private void LoadDailyState()
    {
        _lastRewardDate = PlayerPrefs.GetString("DailyReward_Date", string.Empty);

        _rewardsClaimedToday = PlayerPrefs.GetInt("DailyReward_Count", 0);

        ResetIfNewDay();
    }

    private void SaveDailyState()
    {
        PlayerPrefs.SetString("DailyReward_Date", _lastRewardDate);

        PlayerPrefs.SetInt("DailyReward_Count", _rewardsClaimedToday);

        PlayerPrefs.Save();
    }
}