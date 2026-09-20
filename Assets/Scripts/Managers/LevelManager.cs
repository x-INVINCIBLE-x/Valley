using MoreMountains.Feedbacks;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public MMF_Player levelStartFeedback;

    public void Start()
    {
        if (GameManager.Instance.IsAutoPlayEnabled())
        {
            levelStartFeedback?.PlayFeedbacks();

            GameManager.Instance.SetAutoPlayEnabled(false);
        }
    }

    public void SetAutoPlayStatus(bool isEnabled)
    {
        GameManager.Instance.SetAutoPlayEnabled(isEnabled);
    }
}