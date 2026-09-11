using TMPro;
using UnityEngine;

namespace Valley.Player
{
    public class PlayerDebugUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerGravity playerGravity;
        [SerializeField] private PlayerLauncher playerLauncher;

        [Header("UI")]
        [SerializeField] private TMP_Text timeScaleText;
        [SerializeField] private TMP_Text gravityText;
        [SerializeField] private TMP_Text forceText;
        [SerializeField] private TMP_Text fpsText;

        [Header("Formatting")]
        [SerializeField] private string timeScaleFormat = "Time Scale: {0:0.00}";
        [SerializeField] private string gravityFormat = "Gravity: {0:0.00}";
        [SerializeField] private string forceFormat = "Force: {0:0.00}";
        [SerializeField] private string fpsFormat = "FPS: {0:0}";

        [Header("FPS")]
        [SerializeField] private float fpsUpdateInterval = 0.25f;

        private float _fpsTimer;
        private int _frameCount;
        private float _currentFps;

        private void Awake()
        {

        }

        private void Update()
        {
            UpdateTimeScale();
            UpdateGravity();
            UpdateForce();
            UpdateFPS();
        }

        private void UpdateTimeScale()
        {
            if (timeScaleText == null) return;

            timeScaleText.text = string.Format(
                timeScaleFormat,
                Time.timeScale
            );
        }

        private void UpdateGravity()
        {
            if (gravityText == null || playerGravity == null) return;

            gravityText.text = string.Format(
                gravityFormat,
                playerGravity.CurrentGravityScale
            );
        }

        private void UpdateForce()
        {
            if (forceText == null || playerLauncher == null) return;

            forceText.text = string.Format(
                forceFormat,
                playerLauncher.LastLaunchForce
            );
        }

        private void UpdateFPS()
        {
            if (fpsText == null) return;

            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;

            if (_fpsTimer >= fpsUpdateInterval)
            {
                _currentFps = _frameCount / _fpsTimer;

                _frameCount = 0;
                _fpsTimer = 0f;
            }

            fpsText.text = string.Format(
                fpsFormat,
                _currentFps
            );
        }
    }
}