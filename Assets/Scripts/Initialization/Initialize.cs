using MoreMountains.Feedbacks;
using UnityEngine;

public class Initialize : MonoBehaviour
{
    private bool _initialized;

    public bool Initialized => _initialized;

    [SerializeField] public MMF_Player uninitializedFeedback;
    [SerializeField] public MMF_Player initializedFeedback;

    private void Awake()
    {
        if (!_initialized)
        {
            _initialized = true;

            if (uninitializedFeedback != null)
                uninitializedFeedback.PlayFeedbacks();
        }
        else
        {
            if (initializedFeedback != null)
                initializedFeedback.PlayFeedbacks();
        }
    }
}
