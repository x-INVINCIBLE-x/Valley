using MoreMountains.Feedbacks;
using UnityEngine;

public class PlayInteraction : MonoBehaviour
{
    private const string FirstPlayKey = "FirstPlay";

    [SerializeField] private MMF_Player PlayFeedback;
    [SerializeField] private MMF_Player TutorialPopup;

    public void Play()
    {
        bool isFirstPlay = PlayerPrefs.GetInt(FirstPlayKey, 0) == 0;

        if (isFirstPlay)
        {
            TutorialPopup?.PlayFeedbacks();

            PlayerPrefs.SetInt(FirstPlayKey, 1);
            PlayerPrefs.Save();
        }
        else
        {
            PlayFeedback?.PlayFeedbacks();
        }
    }
}