using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using Valley.Core;

namespace Valley.UI
{
    public class PlayerLaunchChargesUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI chargesText;
        [SerializeField] private PlayerLaunchGate launchGate;
        [SerializeField] private MMF_Player feedback; 

        private void OnEnable()
        {
            PlayerLaunchGate.OnChargesChanged += HandleChargesChanged;

            if (launchGate != null)
            {
                chargesText.text = launchGate.Remaining.ToString();
            }
        }

        private void OnDisable()
        {
            PlayerLaunchGate.OnChargesChanged -= HandleChargesChanged;
        }

        private void HandleChargesChanged(int remaining, int maxCharges)
        {
            chargesText.text = remaining.ToString();
            feedback.PlayFeedbacks();
        }
    }
}