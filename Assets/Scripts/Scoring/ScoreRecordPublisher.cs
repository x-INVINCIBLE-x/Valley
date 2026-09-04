using UnityEngine;

namespace Valley.Scoring
{
    /// <summary>
    /// Reads DistanceScoreTracker's live Score/Distance each frame and pushes them
    /// into PlayerScoreData.
    ///
    /// Persistence is handled by SaveLoad.
    /// </summary>
    public class ScoreRecordPublisher : MonoBehaviour
    {
        [SerializeField] private DistanceScoreTracker tracker;
        [SerializeField] private PlayerScoreData scoreData;

        private void LateUpdate()
        {
            if (tracker == null || scoreData == null)
                return;

            scoreData.SetCurrent(
                tracker.Score,
                tracker.Distance
            );
        }

        public void ResetCurrent()
        {
            if (scoreData == null)
                return;

            scoreData.ResetCurrent();
        }
    }
}
