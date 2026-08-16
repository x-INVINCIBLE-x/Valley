using UnityEngine;

namespace Valley.Scoring
{
    /// <summary>
    /// Reads DistanceScoreTracker's live Score/Distance each frame and pushes them into a
    /// PlayerScoreData blackboard, so UI (and anything else) can consume score without
    /// referencing the tracker directly.
    /// </summary>
    public class ScoreRecordPublisher : MonoBehaviour
    {
        [SerializeField] private DistanceScoreTracker tracker;
        [SerializeField] private PlayerScoreData scoreData;

        private void OnEnable() => scoreData.LoadBest();

        // LateUpdate guarantees DistanceScoreTracker.Update has already run this frame,
        // so the blackboard never lags a frame behind the tracker.
        private void LateUpdate() => scoreData.SetCurrent(tracker.Score, tracker.Distance);
        public void ResetCurrent() => scoreData.ResetCurrent();
    }
}