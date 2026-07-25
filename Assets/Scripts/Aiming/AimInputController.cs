using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Valley.Aiming
{
    public class AimInputController : MonoBehaviour, PlayerControls.IGameplayActions
    {
        public static event Action OnAimStarted;
        public static event Action<Vector3, float> OnAiming;
        public static event Action<Vector3, float> OnAimReleased;

        [SerializeField] private float rotationSpeedDegPerSec = 220f;
        [SerializeField] private float startAngleDeg = 90f;
        [SerializeField] private bool rotationAffectedByTimeScale = true;
        [SerializeField] private float maxChargeTime = 1f;

        private PlayerControls _controls;
        private float _currentAngleDeg;
        private float _holdTimer;
        private bool _isAiming;

        private void Awake()
        {
            _controls = new PlayerControls();
            _controls.Gameplay.SetCallbacks(this);
        }

        private void OnEnable() => _controls.Gameplay.Enable();
        private void OnDisable() => _controls.Gameplay.Disable();
        private void OnDestroy() => _controls.Dispose();

        public void OnAim(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                BeginAim();
            }
            else if (context.canceled)
            {
                EndAim();
            }
        }

        private void Update()
        {
            if (!_isAiming) return;
            TickAim();
        }

        private void BeginAim()
        {
            _isAiming = true;
            _holdTimer = 0f;
            _currentAngleDeg = startAngleDeg;
            OnAimStarted?.Invoke();
        }

        private void TickAim()
        {
            float rotationDt = rotationAffectedByTimeScale ? Time.deltaTime : Time.unscaledDeltaTime;
            _currentAngleDeg = (_currentAngleDeg + rotationSpeedDegPerSec * rotationDt) % 360f;

            _holdTimer += Time.unscaledDeltaTime;
            float charge = Mathf.Clamp01(_holdTimer / maxChargeTime);

            OnAiming?.Invoke(AngleToDir(_currentAngleDeg), charge);
        }

        private void EndAim()
        {
            _isAiming = false;
            float charge = Mathf.Clamp01(_holdTimer / maxChargeTime);
            OnAimReleased?.Invoke(AngleToDir(_currentAngleDeg), charge);
        }

        private static Vector3 AngleToDir(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
        }
    }
}
