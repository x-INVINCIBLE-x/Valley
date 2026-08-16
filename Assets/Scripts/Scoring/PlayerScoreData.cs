using System;
using UnityEngine;

namespace Valley.Scoring
{
    /// <summary>
    /// Runtime score blackboard.
    ///   Current  - live, can dip (score/distance both decrease on backward movement).
    ///   RunPeak  - highest Current has reached during THIS run; only ever rises, resets on ResetCurrent.
    ///   Best     - highest ever reached across runs; persisted to PlayerPrefs.
    ///
    /// All three are single ScoreRecord instances created once and mutated in place - grab
    /// the reference (e.g. in Awake) and it stays live, no re-fetching.
    ///
    /// Update frequency is per-frame and value-driven (no Time/Time.deltaTime involved), so
    /// none of this is affected by Time.timeScale changing mid-run (aim slow-mo, QTEs, etc.) -
    /// Update/LateUpdate still tick every rendered frame regardless of timeScale, and the
    /// comparisons here only ever look at Score/Distance values, never elapsed time.
    ///
    /// Single-session only for now: everything implicitly uses DefaultSessionId. Storage
    /// is already keyed by session internally, so real multi-session support later is just
    /// adding a sessionId parameter to the public API rather than restructuring persistence.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerScoreData", menuName = "Valley/Scoring/Player Score Data")]
    public class PlayerScoreData : ScriptableObject
    {
        private const string DefaultSessionId = "default";
        private const string HighScoreKeyPrefix = "Valley.HighScore.";
        private const string HighDistanceKeyPrefix = "Valley.HighDistance.";

        // <Previous, New>
        public event Action<float, float> OnCurrentScoreChanged;
        public event Action<float, float> OnCurrentDistanceChanged;
        public event Action<float, float> OnRunPeakScoreChanged;
        public event Action<float, float> OnRunPeakDistanceChanged;
        public event Action<float, float> OnHighScoreChanged;
        public event Action<float, float> OnHighDistanceChanged;

        public ScoreRecord Current { get; } = new ScoreRecord();
        public ScoreRecord RunPeak { get; } = new ScoreRecord();
        public ScoreRecord Best { get; } = new ScoreRecord();

        private string _sessionId = DefaultSessionId;
        private bool _loaded;

        private void OnEnable() => _loaded = false;

        /// <summary>Loads the persisted best record into the Best instance. Safe to call more than once.</summary>
        public void LoadBest(string sessionId = DefaultSessionId)
        {
            _sessionId = sessionId;
            Best.Score = PlayerPrefs.GetFloat(HighScoreKeyPrefix + _sessionId, 0f);
            Best.Distance = PlayerPrefs.GetFloat(HighDistanceKeyPrefix + _sessionId, 0f);
            _loaded = true;
        }

        /// <summary>Push the live score/distance for the current run. Call every frame from a publisher.</summary>
        public void SetCurrent(float score, float distance)
        {
            if (!_loaded) LoadBest(_sessionId);

            float previousScore = Current.Score;
            float previousDistance = Current.Distance;

            Current.Score = score;
            Current.Distance = distance;

            if (!Mathf.Approximately(previousScore, score))
                OnCurrentScoreChanged?.Invoke(previousScore, score);

            if (!Mathf.Approximately(previousDistance, distance))
                OnCurrentDistanceChanged?.Invoke(previousDistance, distance);

            TryUpdateRunPeak(score, distance);
            TryUpdateBest(score, distance);
        }

        /// <summary>Zeroes the live run values and this run's peak (e.g. on a new attempt). Best is untouched.</summary>
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

            OnCurrentScoreChanged?.Invoke(previousScore, 0f);
            OnCurrentDistanceChanged?.Invoke(previousDistance, 0f);
            OnRunPeakScoreChanged?.Invoke(previousPeakScore, 0f);
            OnRunPeakDistanceChanged?.Invoke(previousPeakDistance, 0f);
        }

        private void TryUpdateRunPeak(float score, float distance)
        {
            if (score > RunPeak.Score)
            {
                float previous = RunPeak.Score;
                RunPeak.Score = score;
                OnRunPeakScoreChanged?.Invoke(previous, score);
            }

            if (distance > RunPeak.Distance)
            {
                float previous = RunPeak.Distance;
                RunPeak.Distance = distance;
                OnRunPeakDistanceChanged?.Invoke(previous, distance);
            }
        }

        private void TryUpdateBest(float score, float distance)
        {
            float previousScore = Best.Score;
            float previousDistance = Best.Distance;
            bool changed = false;

            if (score > Best.Score) { Best.Score = score; changed = true; }
            if (distance > Best.Distance) { Best.Distance = distance; changed = true; }

            if (!changed) return;

            if (!Mathf.Approximately(previousScore, Best.Score))
                OnHighScoreChanged?.Invoke(previousScore, Best.Score);

            if (!Mathf.Approximately(previousDistance, Best.Distance))
                OnHighDistanceChanged?.Invoke(previousDistance, Best.Distance);

            PlayerPrefs.SetFloat(HighScoreKeyPrefix + _sessionId, Best.Score);
            PlayerPrefs.SetFloat(HighDistanceKeyPrefix + _sessionId, Best.Distance);
            PlayerPrefs.Save();
        }
    }
}