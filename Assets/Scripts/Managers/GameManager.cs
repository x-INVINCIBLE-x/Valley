using MoreMountains.Feedbacks;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Valley.Aiming;

public class GameManager : MonoBehaviour, IAimBlocker
{
    public static GameManager Instance { get; private set; }

    private MMF_Player resetFeedback;

    public event Action<bool> OnPaused;

    public bool IsPaused { get { return isPaused; } }
    public bool CanAim => !isPaused;

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    private void Start()
    {
        InputController.OnPaused += TogglePause;
    }

    private void OnDestroy()
    {
        InputController.OnPaused -= TogglePause;
    }

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

    public void Reset()
    {
        if (resetFeedback != null)
        {
            resetFeedback.PlayFeedbacks();
        }
    }
}