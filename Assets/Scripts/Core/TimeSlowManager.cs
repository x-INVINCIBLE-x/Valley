using System.Collections;
using UnityEngine;
using Valley.Aiming;

namespace Valley.Core
{
    public class TimeSlowManager : MonoBehaviour
    {
        public static TimeSlowManager Instance { get; private set; }

        [SerializeField, Range(0.01f, 1f)] private float slowScale = 0.15f;
        [SerializeField] private float transitionSpeed = 8f;
        [SerializeField] private float fixedDeltaTimeBase = 0.02f;

        private float _targetScale = 1f;
        private Coroutine _routine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            AimInputController.OnAimStarted += SlowDown;
            AimInputController.OnAimReleased += HandleAimReleased;
        }

        private void OnDisable()
        {
            AimInputController.OnAimStarted -= SlowDown;
            AimInputController.OnAimReleased -= HandleAimReleased;
        }

        private void HandleAimReleased(Vector3 direction, float charge) => ResumeNormal();

        public void SlowDown() => SetTarget(slowScale);
        public void ResumeNormal() => SetTarget(1f);

        private void SetTarget(float target)
        {
            _targetScale = target;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            while (Mathf.Abs(Time.timeScale - _targetScale) > 0.001f)
            {
                Time.timeScale = Mathf.MoveTowards(Time.timeScale, _targetScale, transitionSpeed * Time.unscaledDeltaTime);
                Time.fixedDeltaTime = fixedDeltaTimeBase * Time.timeScale;
                yield return null;
            }
            Time.timeScale = _targetScale;
            Time.fixedDeltaTime = fixedDeltaTimeBase * Time.timeScale;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = fixedDeltaTimeBase;
        }
    }
}
