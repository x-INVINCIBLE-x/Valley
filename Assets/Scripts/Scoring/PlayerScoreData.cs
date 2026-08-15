using System;
using UnityEngine;

namespace Valley.Scoring
{
    /// <summary>
    /// Runtime score blackboard. Holds the live run's score/distance plus the best-ever
    /// record, and persists the best record to PlayerPrefs. UI reads/subscribes to this
    /// asset directly instead of referencing DistanceScoreTracker.
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
        public event Action<float, float> OnHighScoreChanged;
        public event Action<float, float> OnHighDistanceChanged;

        public ScoreRecord Current { get; private set; } = ScoreRecord.Zero;
        public ScoreRecord Best { get; private set; } = ScoreRecord.Zero;

        private string _sessionId = DefaultSessionId;
        private bool _loaded;

        private void OnEnable() => _loaded = false;

        /// <summary>Loads the persisted best record. Safe to call more than once; SetCurrent calls it lazily too.</summary>
        public void LoadBest(string sessionId = DefaultSessionId)
        {
            _sessionId = sessionId;
            float highScore = PlayerPrefs.GetFloat(HighScoreKeyPrefix + _sessionId, 0f);
            float highDistance = PlayerPrefs.GetFloat(HighDistanceKeyPrefix + _sessionId, 0f);
            Best = new ScoreRecord(highScore, highDistance);
            _loaded = true;
        }

        /// <summary>Push the live score/distance for the current run. Call every frame from a publisher.</summary>
        public void SetCurrent(float score, float distance)
        {
            if (!_loaded) LoadBest(_sessionId);

            ScoreRecord previous = Current;
            Current = new ScoreRecord(score, distance);

            if (!Mathf.Approximately(previous.Score, score))
                OnCurrentScoreChanged?.Invoke(previous.Score, score);

            if (!Mathf.Approximately(previous.Distance, distance))
                OnCurrentDistanceChanged?.Invoke(previous.Distance, distance);

            TryUpdateBest(score, distance);
        }

        /// <summary>Zeroes the live run values (e.g. on a new attempt). Best is untouched.</summary>
        public void ResetCurrent()
        {
            ScoreRecord previous = Current;
            Current = ScoreRecord.Zero;
            OnCurrentScoreChanged?.Invoke(previous.Score, 0f);
            OnCurrentDistanceChanged?.Invoke(previous.Distance, 0f);
        }

        private void TryUpdateBest(float score, float distance)
        {
            ScoreRecord previous = Best;
            float bestScore = previous.Score;
            float bestDistance = previous.Distance;
            bool changed = false;

            if (score > bestScore) { bestScore = score; changed = true; }
            if (distance > bestDistance) { bestDistance = distance; changed = true; }

            if (!changed) return;

            Best = new ScoreRecord(bestScore, bestDistance);

            if (!Mathf.Approximately(previous.Score, bestScore))
                OnHighScoreChanged?.Invoke(previous.Score, bestScore);

            if (!Mathf.Approximately(previous.Distance, bestDistance))
                OnHighDistanceChanged?.Invoke(previous.Distance, bestDistance);

            PlayerPrefs.SetFloat(HighScoreKeyPrefix + _sessionId, bestScore);
            PlayerPrefs.SetFloat(HighDistanceKeyPrefix + _sessionId, bestDistance);
            PlayerPrefs.Save();
        }
    }
}