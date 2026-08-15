namespace Valley.Scoring
{
    /// <summary>
    /// Immutable score/distance pairing. Used for both the live "current" run
    /// and the persisted "best" record.
    /// </summary>
    public readonly struct ScoreRecord
    {
        public readonly float Score;
        public readonly float Distance;

        public ScoreRecord(float score, float distance)
        {
            Score = score;
            Distance = distance;
        }

        public static readonly ScoreRecord Zero = new ScoreRecord(0f, 0f);
    }
}