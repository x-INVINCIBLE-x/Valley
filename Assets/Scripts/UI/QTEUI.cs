using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using Valley.QTE;

public class QTEUI : MonoBehaviour
{
    [Header("Progress Bars")]
    [SerializeField] private MMProgressBar timeRadialBar;
    [SerializeField] private MMProgressBar progressRadialBar;

    [Header("Feedbacks")]
    [SerializeField] private MMF_Player qteStartFeedback;
    [SerializeField] private MMF_Player qteTapFeedback;
    [SerializeField] private MMF_Player qteSuccessFeedback;
    [SerializeField] private MMF_Player qteFailFeedback;

    [Header("UI Update")]
    [SerializeField, Min(0f)]
    private float cooldown = 0.1f; // Update UI every 0.1 seconds

    private bool isActive = false;
    private float currentTime = 0f;
    private float endTime = 0f;
    private float lastUpdate = 0f;

    private void OnEnable()
    {
        QuickTimeEventRunner.OnQTEStarted += HandleQTEStarted;
        QuickTimeEventRunner.OnQTETapRegistered += HandleQTETapRegistered;
        QuickTimeEventRunner.OnQTESucceeded += HandleQTESucceeded;
        QuickTimeEventRunner.OnQTEFailed += HandleQTEFailed;
    }

    private void OnDisable()
    {
        QuickTimeEventRunner.OnQTEStarted -= HandleQTEStarted;
        QuickTimeEventRunner.OnQTETapRegistered -= HandleQTETapRegistered;
        QuickTimeEventRunner.OnQTESucceeded -= HandleQTESucceeded;
        QuickTimeEventRunner.OnQTEFailed -= HandleQTEFailed;
    }

    private void Update()
    {
        if (!isActive)
            return;

        currentTime += Time.deltaTime;

        // Throttle UI updates
        if (Time.time >= lastUpdate + cooldown)
        {
            lastUpdate = Time.time;
            timeRadialBar.UpdateBar(currentTime, 0f, endTime);
        }
    }

    private void HandleQTEStarted(QuickTimeEventProfile profile)
    {
        currentTime = 0f;
        endTime = profile.duration;
        lastUpdate = Time.time;

        // Initialize bars
        timeRadialBar.UpdateBar(0f, 0f, endTime);
        progressRadialBar.UpdateBar(0f, 0f, profile.requiredTaps);

        qteStartFeedback?.PlayFeedbacks();

        isActive = true;
    }

    private void HandleQTETapRegistered(int tapsDone, int requiredTaps)
    {
        progressRadialBar.UpdateBar(tapsDone, 0f, requiredTaps);

        qteTapFeedback?.PlayFeedbacks();
    }

    private void HandleQTESucceeded()
    {
        isActive = false;

        // Fill the timer on success if desired
        timeRadialBar.UpdateBar(endTime, 0f, endTime);

        qteSuccessFeedback?.PlayFeedbacks();
    }

    private void HandleQTEFailed()
    {
        isActive = false;

        qteFailFeedback?.PlayFeedbacks();
    }
}