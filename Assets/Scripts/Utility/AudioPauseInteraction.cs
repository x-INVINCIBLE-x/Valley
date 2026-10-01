using MoreMountains.Feedbacks;
using System.Collections;
using UnityEngine;
using Valley.Player;

public class AudioPauseInteraction : MonoBehaviour
{
    [SerializeField] private MMF_Player pauseFeedback;

    private Coroutine pauseRoutine;

    private void Start()
    {
        GameManager.Instance.OnPaused += HandlePause;
        PlayerHealth.OnPlayerDied += HandleDeath;
        PlayerHealth.OnPlayerRevived += HandleRevive;
        PlayerHealth.OnPlayerDamaged += TemporaryPause;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnPaused -= HandlePause;
        PlayerHealth.OnPlayerDied -= HandleDeath;
        PlayerHealth.OnPlayerRevived -= HandleRevive;
        PlayerHealth.OnPlayerDamaged -= TemporaryPause;
    }

    private void HandleRevive()
    {
        HandlePause(false);
    }

    private void HandleDeath()
    {
        HandlePause(true);
    }

    private void HandlePause(bool state)
    {
        if (state)
        {
            pauseFeedback.PlayFeedbacks();
        }
        else
        {
            pauseFeedback.StopFeedbacks();
        }
    }

    private void TemporaryPause()
    {
        if (pauseRoutine != null)
        {
            StopCoroutine(pauseRoutine);
        }

        pauseRoutine = StartCoroutine(TempPause());
    }

    private IEnumerator TempPause()
    {
        HandlePause(true);
        yield return new WaitForSeconds(1f);
        HandlePause(false);

        pauseRoutine = null;
    }
}