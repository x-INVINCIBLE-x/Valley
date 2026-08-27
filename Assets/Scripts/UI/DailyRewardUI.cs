using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyRewardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button rewardButton;
    [SerializeField] private TMP_Text rewardProgressText;

    private DailyReward _dailyReward;

    public void Initialize(DailyReward dailyReward)
    {
        if (_dailyReward != null)
        {
            Unsubscribe();
        }

        _dailyReward = dailyReward;

        if (_dailyReward == null)
        {
            Debug.LogError(
                "DailyRewardUI: DailyReward is null.",
                this);

            return;
        }

        Subscribe();

        UpdateProgress(
            _dailyReward.RewardsClaimedToday,
            _dailyReward.MaxRewards);

        UpdateButtonState();
    }

    private void Awake()
    {
        if (rewardButton != null)
        {
            rewardButton.onClick.AddListener(HandleRewardClicked);
        }
    }

    private void HandleRewardClicked()
    {
        if (_dailyReward == null)
            return;

        if (_dailyReward.RemainingAttempts <= 0)
            return;

        rewardButton.interactable = false;

        _dailyReward.HandleReward();
    }

    private void HandleRewardSuccess()
    {
        UpdateButtonState();
    }

    private void HandleRewardFailure(string reason)
    {
        Debug.Log(reason);

        UpdateButtonState();
    }

    private void HandleProgressChanged(
        int watched,
        int total)
    {
        UpdateProgress(watched, total);
        UpdateButtonState();
    }

    private void UpdateProgress(
        int watched,
        int total)
    {
        if (rewardProgressText == null)
            return;

        rewardProgressText.text = $"{watched} / {total}";
    }

    private void UpdateButtonState()
    {
        if (rewardButton == null)
            return;

        if (_dailyReward == null)
        {
            rewardButton.interactable = false;
            return;
        }

        rewardButton.interactable =
            _dailyReward.RemainingAttempts > 0;
    }

    private void Subscribe()
    {
        _dailyReward.OnRewardSuccess +=
            HandleRewardSuccess;

        _dailyReward.OnRewardFailure +=
            HandleRewardFailure;

        _dailyReward.OnProgressChanged +=
            HandleProgressChanged;
    }

    private void Unsubscribe()
    {
        _dailyReward.OnRewardSuccess -=
            HandleRewardSuccess;

        _dailyReward.OnRewardFailure -=
            HandleRewardFailure;

        _dailyReward.OnProgressChanged -=
            HandleProgressChanged;
    }

    private void OnDestroy()
    {
        if (_dailyReward != null)
        {
            Unsubscribe();
        }

        if (rewardButton != null)
        {
            rewardButton.onClick.RemoveListener(
                HandleRewardClicked);
        }
    }
}