using MoreMountains.Feedbacks;
using System;
using UnityEngine;
using Valley.Powerups;

public class PowerupFeedback : MonoBehaviour
{
    [Serializable]
    public class Feedback
    {
        public PowerupEffect Effect;
        public MMF_Player TargetFeedback;
    }

    public Feedback[] feedbacks;

    private void Start()
    {
        PowerupReceiver.OnPowerupActivated += HandlePowerupActivated;
    }

    private void HandlePowerupActivated(PowerupEffect effect)
    {
        for (int i = 0; i < feedbacks.Length; i++)
        {
            if (feedbacks[i].Effect == effect)
            {
                feedbacks[i].TargetFeedback?.PlayFeedbacks();
                break;
            }
        }
    }

    private void OnDestroy()
    {
        PowerupReceiver.OnPowerupActivated -= HandlePowerupActivated;
    }
}
