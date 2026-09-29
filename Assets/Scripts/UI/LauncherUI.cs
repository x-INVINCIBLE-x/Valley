using MoreMountains.Feedbacks;
using System.Collections.Generic;
using UnityEngine;
using Valley.Aiming;
using Valley.Core;

public class LauncherUI : MonoBehaviour
{
    [SerializeField] private LaunchesUI prefab;
    [SerializeField] private Transform container;
    [SerializeField] private PlayerLaunchGate launchGate;

    [SerializeField] private float deltaSize = 0.1f;

    [Header("Feedbacks")]
    [SerializeField] private MMF_Player nolaunchFeedback;

    private readonly List<LaunchesUI> launchesUI = new();
    private int activeCount = 0;

    private void Start()
    {
        InputController.OnAimStarted += HandleAimStarted;
        InputController.OnAimReleased += HandleAimReleased;
        InputController.OnAimCancelled += HandleAimCancelled;
        InputController.NoAim += HandleNoAim;
    }

    private void OnDestroy()
    {
        InputController.OnAimStarted -= HandleAimStarted;
        InputController.OnAimReleased -= HandleAimReleased;
        InputController.OnAimCancelled -= HandleAimCancelled;
        InputController.NoAim -= HandleNoAim;
    }

    private void HandleAimStarted()
    {
        int launches = launchGate != null ? launchGate.Remaining : 0;
        activeCount = launches;

        if (launches <= 0)
        {
            nolaunchFeedback?.PlayFeedbacks();
            return;
        }

        EnsurePoolSize(launches);

        for (int i = 0; i < launchesUI.Count; i++)
        {
            bool shouldBeActive = i < launches;

            launchesUI[i].gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
                continue;

            // Alternate rotation direction:
            // 0 = clockwise
            // 1 = counter-clockwise
            bool clockwise = i % 2 == 0;

            launchesUI[i].HandleAimStarted(clockwise);
        }
    }

    private void HandleAimReleased(Vector3 vector, float arg2)
    {
        for (int i = 0; i < activeCount; i++)
        {
            launchesUI[i].HandleAimReleased(vector, arg2);
        }

        activeCount = 0;
    }

    private void HandleAimCancelled()
    {
        for (int i = 0; i < activeCount; i++)
        {
            launchesUI[i].HandleAimCancelled();
        }

        activeCount = 0;
    }

    private void HandleNoAim()
    {
        if (launchGate != null && launchGate.Remaining <= 0)
        {
            nolaunchFeedback?.PlayFeedbacks();
        }
    }

    private void EnsurePoolSize(int requiredCount)
    {
        while (launchesUI.Count < requiredCount)
        {
            LaunchesUI instance = Instantiate(prefab, container);
            instance.gameObject.SetActive(false);

            float scaleMultiplier = 1f + (deltaSize * launchesUI.Count);

            instance.transform.localScale *= scaleMultiplier;

            launchesUI.Add(instance);
        }
    }
}