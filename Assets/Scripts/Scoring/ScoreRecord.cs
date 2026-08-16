namespace Valley.Scoring
{
    /// <summary>
    /// Mutable score/distance pairing. PlayerScoreData creates one instance for "current"
    /// and one for "best" and updates their fields in place - anything that holds a
    /// reference to one (e.g. cached once in Awake) always sees the live values without
    /// needing to re-fetch from PlayerScoreData.
    /// </summary>
    public class ScoreRecord
    {
        public float Score { get; internal set; }
        public float Distance { get; internal set; }

        public ScoreRecord() : this(0f, 0f) { }

        public ScoreRecord(float score, float distance)
        {
            Score = score;
            Distance = distance;
        }
    }
}