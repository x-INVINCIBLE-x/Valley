using MoreMountains.Feedbacks;
using UnityEngine;
using Valley.Aiming;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private MMF_Player pauseFeedback;
    [SerializeField] private MMF_Player unpauseFeedback;

    private void Start()
    {
        GameManager.Instance.OnPaused += ShowPauseUI;

        pauseUI.SetActive(false);
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnPaused -= ShowPauseUI;
    }

    private void ShowPauseUI(bool isPaused)
    {
        Debug.Log($"PauseUI: ShowPauseUI called with isPaused = {isPaused}");
        if (isPaused && pauseFeedback != null)
            pauseFeedback.PlayFeedbacks();
        else if (!isPaused && unpauseFeedback != null)
            unpauseFeedback.PlayFeedbacks();

        pauseUI.SetActive(isPaused);
    }

    public void Pause()
    {
        GameManager.Instance.SetPause(true);
    }

    public void Resume()
    {
        GameManager.Instance.SetPause(false);
    }
}