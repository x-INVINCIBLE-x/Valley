using UnityEngine;
using UnityEngine.UI;
using Valley.Revive;
using Valley.Player;
using MoreMountains.Feedbacks;

namespace Valley.UI
{
    public class ReviveUI: MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerReviveController reviveController;
        [SerializeField] private MMF_Player reviveFeedback;

        [Header("Revive Offer")]
        [SerializeField] private GameObject revivePanel;
        [SerializeField] private Text countdownText;
        [SerializeField] private Image countdownFillImage;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button declineButton;

        [Header("End Screen")]
        [SerializeField] private GameObject[] endScreenPanels;

        [Header("Cleanup")]
        [SerializeField] private GameObject[] toDisableOnEnd;

        private void Awake()
        {
            SetActive(revivePanel, false);

            for (int i = 0; i < endScreenPanels.Length; i++)
            {
                SetActive(endScreenPanels[i], false);
            }
        }

        private void OnEnable()
        {
            PlayerReviveController.OnReviveOfferStarted += HandleOfferStarted;
            PlayerReviveController.OnReviveCountdownTick += HandleCountdownTick;
            PlayerReviveController.OnGameOver += HandleGameOver;
            PlayerHealth.OnPlayerRevived += HandleRevived;

            if (watchAdButton != null) watchAdButton.onClick.AddListener(HandleWatchAdClicked);
            if (declineButton != null) declineButton.onClick.AddListener(HandleDeclineClicked);
        }

        private void OnDisable()
        {
            PlayerReviveController.OnReviveOfferStarted -= HandleOfferStarted;
            PlayerReviveController.OnReviveCountdownTick -= HandleCountdownTick;
            PlayerReviveController.OnGameOver -= HandleGameOver;
            PlayerHealth.OnPlayerRevived -= HandleRevived;

            if (watchAdButton != null) watchAdButton.onClick.RemoveListener(HandleWatchAdClicked);
            if (declineButton != null) declineButton.onClick.RemoveListener(HandleDeclineClicked);
        }

        private void HandleOfferStarted(float duration)
        {
            SetActive(revivePanel, true);

            for (int i = 0; i < toDisableOnEnd.Length; i++)
            {
                SetActive(toDisableOnEnd[i], false);
            }
        }

        private void HandleCountdownTick(float secondsRemaining, float normalizedRemaining)
        {
            if (countdownText != null) countdownText.text = Mathf.CeilToInt(secondsRemaining).ToString();
            if (countdownFillImage != null) countdownFillImage.fillAmount = normalizedRemaining;
        }

        private void HandleRevived()
        {
            SetActive(revivePanel, false);

            for (int i = 0; i < toDisableOnEnd.Length; i++)
            {
                SetActive(toDisableOnEnd[i], true);
            }

            reviveFeedback?.PlayFeedbacks();
        }

        private void HandleGameOver()
        {
            SetActive(revivePanel, false);
            for (int i = 0; i < endScreenPanels.Length; i++)
            {
                SetActive(endScreenPanels[i], true);
            }
        }

        private void HandleWatchAdClicked()
        {
            if (reviveController != null) reviveController.RequestRevive();
        }

        private void HandleDeclineClicked()
        {
            if (reviveController != null) reviveController.DeclineRevive();
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}