using UnityEngine;
using Valley.Economy;

public class DailyRewardController : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private int rewardAmount = 10;

    [Header("Daily Limit")]
    [SerializeField, Min(1)] private int maxRewardsPerDay = 3;

    [Header("UI")]
    [SerializeField] private DailyRewardUI rewardUI;

    private CurrencyWallet wallet;
    private DailyReward _dailyReward;

    private void Start()
    {
        wallet = CurrencyWallet.Instance;

        if (wallet == null)
        {
            Debug.LogError("DailyRewardController: CurrencyWallet is not assigned.");
            return;
        }

        _dailyReward = new DailyReward(
            wallet,
            rewardAmount,
            maxRewardsPerDay
        );

        if (rewardUI != null)
        {
            rewardUI.Initialize(_dailyReward);
        }
    }
}