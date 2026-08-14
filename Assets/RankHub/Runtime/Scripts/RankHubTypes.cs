using System;

namespace RankHub
{
    [Serializable]
    public class ScoreEntry
    {
        public string player;
        public int score;
        public string player_id;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public string leaderboard;
        public string plan;
        public int page;
        public int limit;
        public int total_scores;
        public ScoreEntry[] scores;
    }

    [Serializable]
    public class AddScoreResponse
    {
        public bool success;
        public string message;
        public string plan;
        public int scores_used;
        public int scores_limit;
        public string replaced_player;
        public int replaced_score;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string error;
    }
}