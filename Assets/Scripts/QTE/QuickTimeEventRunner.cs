using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Valley.Aiming;

namespace Valley.QTE
{
    public class QuickTimeEventRunner : MonoBehaviour, PlayerControls.IGameplayActions, IAimBlocker
    {
        public static QuickTimeEventRunner Instance { get; private set; }

        public static event Action<QuickTimeEventProfile> OnQTEStarted;
        //<tapsDone, requiredTaps>
        public static event Action<int, int> OnQTETapRegistered;
        public static event Action OnQTESucceeded;
        public static event Action OnQTEFailed;

        private PlayerControls _controls;
        private Coroutine _timeoutRoutine;
        private QuickTimeEventProfile _activeProfile;
        private int _tapsDone;

        public bool IsActive { get; private set; }
        bool IAimBlocker.CanAim => !IsActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _controls = new PlayerControls();
            _controls.Gameplay.SetCallbacks(this);
        }

        private void OnEnable() => _controls.Gameplay.Enable();
        private void OnDisable() => _controls.Gameplay.Disable();
        private void OnDestroy() => _controls.Dispose();

        public bool Begin(QuickTimeEventProfile profile)
        {
            if (IsActive || profile == null) return false;

            _activeProfile = profile;
            _tapsDone = 0;
            IsActive = true;

            OnQTEStarted?.Invoke(profile);
            OnQTETapRegistered?.Invoke(_tapsDone, profile.requiredTaps);

            _timeoutRoutine = StartCoroutine(TimeoutRoutine(profile.duration));
            return true;
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            if (!IsActive || !context.started) return;

            _tapsDone++;
            OnQTETapRegistered?.Invoke(_tapsDone, _activeProfile.requiredTaps);

            if (_tapsDone >= _activeProfile.requiredTaps)
            {
                Finish(success: true);
            }
        }

        private IEnumerator TimeoutRoutine(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            Finish(success: false);
        }

        private void Finish(bool success)
        {
            if (!IsActive) return;

            IsActive = false;
            if (_timeoutRoutine != null) StopCoroutine(_timeoutRoutine);

            if (success) OnQTESucceeded?.Invoke();
            else OnQTEFailed?.Invoke();
        }

        public void OnPause(InputAction.CallbackContext context)
        {
        }
    }
}