using Esper.ESave.Example;
using MoreMountains.Feedbacks;
using System;
using UnityEngine;
using Valley.Aiming;
using Valley.Revive;
using Valley.Theming;

public class GameManager : MonoBehaviour, IAimBlocker
{
    public static GameManager Instance { get; private set; }

    [SerializeField]
    private SaveLoad saveLoad;

    private MMF_Player resetFeedback;

    public event Action<bool> OnPaused;

    public bool IsPaused => isPaused;

    public bool CanAim => !isPaused;

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(null);
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (saveLoad == null)
        {
            saveLoad = GetComponentInChildren<SaveLoad>();
        }

        if (saveLoad == null)
        {
            Debug.LogError(
                "GameManager: SaveLoad is missing."
            );
        }
    }

    private void Start()
    {
        InputController.OnPaused += TogglePause;

        PlayerReviveController.OnGameOver += HandleGameOver;

        LoadGame();
    }

    private void OnDestroy()
    {
        InputController.OnPaused -= TogglePause;
        PlayerReviveController.OnGameOver -= HandleGameOver;
    }

    // ==================================================
    // SAVE / LOAD
    // ==================================================

    public void LoadGame()
    {
        if (saveLoad == null)
            return;

        saveLoad.LoadGame();
    }

    public void SaveGame()
    {
        if (saveLoad == null)
            return;

        saveLoad.SaveGame();
    }

    public void SaveGameToCloud()
    {
        if (saveLoad == null)
            return;

        saveLoad.SaveGameToCloud();
    }

    // ==================================================
    // MATCH END
    // ==================================================

    private void HandleGameOver()
    {
        Debug.Log("Match ended. Saving game.");

        ThemeManager themeManager = ThemeManager.Instance;

        if (themeManager != null)
        {
            themeManager.RevertExpiredTemporaryTheme();
        }

        SaveGame();
    }

    // ==================================================
    // APPLICATION
    // ==================================================

    private void OnApplicationQuit()
    {
        SaveGameToCloud();
    }

    // ==================================================
    // PAUSE
    // ==================================================

    public void TogglePause()
    {
        isPaused = !isPaused;

        OnPaused?.Invoke(isPaused);
    }

    public void SetPause(bool pause)
    {
        isPaused = pause;

        OnPaused?.Invoke(isPaused);
    }

    // ==================================================
    // RESET
    // ==================================================

    public void Reset()
    {
        if (resetFeedback != null)
        {
            resetFeedback.PlayFeedbacks();
        }
    }

    // ==================================================
    // EXIT
    // ==================================================

    public void Exit()
    {
        SaveGameToCloud();

        Application.Quit();
    }
}