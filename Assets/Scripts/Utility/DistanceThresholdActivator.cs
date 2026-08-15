using UnityEngine;

namespace Valley.Scoring
{
    public class DistanceGameObjectActivator : MonoBehaviour
    {
        [SerializeField] private DistanceScoreTracker distanceTracker;
        [SerializeField] private GameObject targetObject;

        [Header("Threshold")]
        [SerializeField] private float threshold;
        [SerializeField] private bool enableAboveThreshold = true;

        private bool _lastState;

        private void Update()
        {
            if (distanceTracker == null || targetObject == null)
                return;

            bool shouldEnable = enableAboveThreshold
                ? distanceTracker.Distance >= threshold
                : distanceTracker.Distance <= threshold;

            if (shouldEnable == _lastState)
                return;

            _lastState = shouldEnable;
            targetObject.SetActive(shouldEnable);
        }
    }
}