using System;
using UnityEngine;

namespace Valley.Scoring
{
    /// <summary>
    /// Runtime score blackboard.
    ///
    /// Current  - live score/distance for the current run. Can decrease.
    /// RunPeak  - highest score/distance reached during the current run.
    /// Best     - highest score/distance reached across all runs.
    ///
    /// PlayerScoreData contains runtime state only.
    /// Local/cloud persistence is handled by SaveLoad.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerScoreData", menuName = "Valley/Scoring/Player Score Data")]
    public class PlayerScoreData : ScriptableObject
    {
        public event Action<float, float> OnCurrentScoreChanged;
        public event Action<float, float> OnCurrentDistanceChanged;
        public event Action<float, float> OnRunPeakScoreChanged;
        public event Action<float, float> OnRunPeakDistanceChanged;
        public event Action<float, float> OnHighScoreChanged;
        public event Action<float, float> OnHighDistanceChanged;

        public ScoreRecord Current { get; } = new ScoreRecord();
        public ScoreRecord RunPeak { get; } = new ScoreRecord();
        public ScoreRecord Best { get; } = new ScoreRecord();

        public float HighScore => Best.Score;

        public float HighDistance => Best.Distance;

        /// <summary>
        /// Push the live score/distance for the current run.
        /// The best score/distance can only increase.
        /// </summary>
        public void SetCurrent(float score, float distance)
        {
            float previousScore = Current.Score;
            float previousDistance = Current.Distance;

            Current.Score = score;
            Current.Distance = distance;

            if (!Mathf.Approximately(previousScore, score))
            {
                OnCurrentScoreChanged?.Invoke(
                    previousScore,
                    score
                );
            }

            if (!Mathf.Approximately(previousDistance, distance))
            {
                OnCurrentDistanceChanged?.Invoke(
                    previousDistance,
                    distance
                );
            }

            TryUpdateRunPeak(
                score,
                distance
            );

            TryUpdateBest(
                score,
                distance
            );
        }

        /// <summary>
        /// Zeroes the live run values and this run's peak.
        /// Best remains untouched.
        /// </summary>
        public void ResetCurrent()
        {
            float previousScore = Current.Score;
            float previousDistance = Current.Distance;
            float previousPeakScore = RunPeak.Score;
            float previousPeakDistance = RunPeak.Distance;

            Current.Score = 0f;
            Current.Distance = 0f;
            RunPeak.Score = 0f;
            RunPeak.Distance = 0f;

            OnCurrentScoreChanged?.Invoke(
                previousScore,
                0f
            );

            OnCurrentDistanceChanged?.Invoke(
                previousDistance,
                0f
            );

            OnRunPeakScoreChanged?.Invoke(
                previousPeakScore,
                0f
            );

            OnRunPeakDistanceChanged?.Invoke(
                previousPeakDistance,
                0f
            );
        }

        /// <summary>
        /// Restores persisted best score/distance without ever lowering
        /// an already existing best value.
        /// </summary>
        public void RestoreBest(
            float score,
            float distance)
        {
            if (score < 0f)
                score = 0f;

            if (distance < 0f)
                distance = 0f;

            TrySetBestScore(score);
            TrySetBestDistance(distance);
        }

        /// <summary>
        /// Replaces the current best values with persisted values.
        /// Use only when the persisted source is authoritative.
        /// </summary>
        public void ReplaceBest(
            float score,
            float distance)
        {
            if (score < 0f)
                score = 0f;

            if (distance < 0f)
                distance = 0f;

            float previousScore = Best.Score;
            float previousDistance = Best.Distance;

            Best.Score = score;
            Best.Distance = distance;

            if (!Mathf.Approximately(
                    previousScore,
                    Best.Score))
            {
                OnHighScoreChanged?.Invoke(
                    previousScore,
                    Best.Score
                );
            }

            if (!Mathf.Approximately(
                    previousDistance,
                    Best.Distance))
            {
                OnHighDistanceChanged?.Invoke(
                    previousDistance,
                    Best.Distance
                );
            }
        }

        private void TryUpdateRunPeak(
            float score,
            float distance)
        {
            if (score > RunPeak.Score)
            {
                float previous = RunPeak.Score;

                RunPeak.Score = score;

                OnRunPeakScoreChanged?.Invoke(
                    previous,
                    score
                );
            }

            if (distance > RunPeak.Distance)
            {
                float previous = RunPeak.Distance;

                RunPeak.Distance = distance;

                OnRunPeakDistanceChanged?.Invoke(
                    previous,
                    distance
                );
            }
        }

        private void TryUpdateBest(
            float score,
            float distance)
        {
            TrySetBestScore(score);
            TrySetBestDistance(distance);
        }

        private void TrySetBestScore(float score)
        {
            if (score <= Best.Score)
                return;

            float previousScore = Best.Score;

            Best.Score = score;

            if (!Mathf.Approximately(
                    previousScore,
                    Best.Score))
            {
                OnHighScoreChanged?.Invoke(
                    previousScore,
                    Best.Score
                );
            }
        }

        private void TrySetBestDistance(float distance)
        {
            if (distance <= Best.Distance)
                return;

            float previousDistance = Best.Distance;

            Best.Distance = distance;

            if (!Mathf.Approximately(
                    previousDistance,
                    Best.Distance))
            {
                OnHighDistanceChanged?.Invoke(
                    previousDistance,
                    Best.Distance
                );
            }
        }
    }
}