using MoreMountains.Feedbacks;
using System;
using TMPro;
using UnityEngine;

public class ChallengeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChallengeZoneDetector challengeZoneDetector;

    [Header("Text Fields")]
    [SerializeField] private string textTemplate = "CHALLENGE ";
    [SerializeField] private TextMeshProUGUI challengeStartText;
    [SerializeField] private TextMeshProUGUI challengeEndText;

    [Header("Feedbacks")]
    [SerializeField] private MMF_Player challengeStartFeedback;
    [SerializeField] private MMF_Player challengeEndFeedback;

    private int challengeStartCount = 0;

    private void Start()
    {
        challengeZoneDetector.ChallengeZoneState += OnChallengeZoneStateChanged;
    }

    private void OnDestroy()
    {
        challengeZoneDetector.ChallengeZoneState -= OnChallengeZoneStateChanged;
    }

    private void OnChallengeZoneStateChanged(bool state)
    {
        if (state)
        {
            challengeStartCount++;
            challengeStartText.text = $"{textTemplate}{challengeStartCount}";
            challengeStartFeedback?.PlayFeedbacks();
        }
        else
        {
            challengeEndText.text = $"{textTemplate}{challengeStartCount}";
            challengeEndFeedback?.PlayFeedbacks();
        }
    }
}
