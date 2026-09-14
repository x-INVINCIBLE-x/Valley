using MoreMountains.Feedbacks;
using UnityEngine;

namespace Valley.QTE
{
    public class QTETutorialTrigger : MonoBehaviour
    {
        [Header("Tutorial Settings")]

        [Tooltip("Maximum number of times this tutorial can be triggered.")]
        [Min(1)]
        [SerializeField] private int maxCalls = 1;

        [Tooltip("Unique PlayerPrefs key used to save this tutorial's call count.")]
        [SerializeField] private string playerPrefsKey = "QTE_Tutorial_01";

        [Header("Tutorial Feedback")]

        [Tooltip("Feedback played when the tutorial is triggered.")]
        [SerializeField] private MMF_Player onQTETriggered;
        [SerializeField] private MMF_Player onQTEEnded;

        private int callCount;

        private void Awake()
        {
            LoadCallCount();
        }

        private void Start()
        {
            QuickTimeEventRunner.OnQTEStarted += HandleQTEStarted;
            QuickTimeEventRunner.OnQTESucceeded += HandleQTEEnded;
            QuickTimeEventRunner.OnQTEFailed += HandleQTEEnded;
        }

        private void OnDestroy()
        {
            QuickTimeEventRunner.OnQTEStarted -= HandleQTEStarted;
            QuickTimeEventRunner.OnQTESucceeded -= HandleQTEEnded;
            QuickTimeEventRunner.OnQTEFailed -= HandleQTEEnded;
        }

        /// <summary>
        /// Called when a QTE succeeds.
        /// </summary>
        private void HandleQTEStarted(QuickTimeEventProfile profile)
        {
            // Nothing to do if the tutorial has reached its limit.
            if (callCount >= maxCalls)
                return;

            callCount++;

            SaveCallCount();

            onQTETriggered?.PlayFeedbacks();
        }

        /// <summary>
        /// Called when a QTE fails.
        /// Stops the tutorial feedback immediately.
        /// </summary>
        private void HandleQTEEnded()
        {
            StopTutorialFeedback();
        }

        /// <summary>
        /// Stops the tutorial feedback.
        /// </summary>
        private void StopTutorialFeedback()
        {
            if (onQTETriggered == null)
                return;

            onQTETriggered.StopFeedbacks();
            onQTEEnded?.PlayFeedbacks();
        }

        private void LoadCallCount()
        {
            callCount = PlayerPrefs.GetInt(playerPrefsKey, 0);

            // Protect against invalid saved values if maxCalls
            // was reduced after the value was saved.
            callCount = Mathf.Clamp(callCount, 0, maxCalls);
        }

        private void SaveCallCount()
        {
            PlayerPrefs.SetInt(playerPrefsKey, callCount);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Resets this tutorial's saved trigger count.
        /// </summary>
        public void ResetTriggerCount()
        {
            callCount = 0;

            PlayerPrefs.SetInt(playerPrefsKey, 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Deletes this tutorial's saved PlayerPrefs data.
        /// </summary>
        public void DeleteSavedProgress()
        {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();

            callCount = 0;
        }

        /// <summary>
        /// Returns true if this tutorial can still be triggered.
        /// </summary>
        public bool CanTrigger()
        {
            return callCount < maxCalls;
        }

        /// <summary>
        /// Returns the current number of tutorial calls.
        /// </summary>
        public int GetCallCount()
        {
            return callCount;
        }

        /// <summary>
        /// Returns the maximum number of allowed calls.
        /// </summary>
        public int GetMaxCalls()
        {
            return maxCalls;
        }
    }
}