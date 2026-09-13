using MoreMountains.Feedbacks;
using System;
using TMPro;
using UnityEngine;
using Valley.Core;

namespace Valley.UI
{
    public class PlayerLaunchChargesUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI chargesText;
        [SerializeField] private PlayerLaunchGate launchGate;

        [Header("Feedbacks")]
        [SerializeField] private MMF_Player feedback;
        [SerializeField] private MMF_Player maxChargesIncFeedback;
        [SerializeField] private MMF_Player maxChargesDecFeedback;

        private void OnEnable()
        {
            PlayerLaunchGate.OnChargesChanged += HandleChargesChanged;
            PlayerLaunchGate.OnMaxChargesChanged += HandleMaxChargesChanged;

            if (launchGate != null)
            {
                chargesText.text = launchGate.Remaining.ToString();
            }
        }

        private void OnDisable()
        {
            PlayerLaunchGate.OnChargesChanged -= HandleChargesChanged;
            PlayerLaunchGate.OnMaxChargesChanged -= HandleMaxChargesChanged;
        }

        private void HandleChargesChanged(int remaining, int maxCharges)
        {
            chargesText.text = remaining.ToString();
            feedback.PlayFeedbacks();
        }

        private void HandleMaxChargesChanged(int prevCharges, int newMaxCharges)
        {
            if (newMaxCharges > prevCharges)
            {
                maxChargesIncFeedback.PlayFeedbacks();
            }
            else if (newMaxCharges < prevCharges)
            {
                maxChargesDecFeedback.PlayFeedbacks();
            }
        }
    }
}