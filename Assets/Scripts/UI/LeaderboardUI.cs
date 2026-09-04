using UnityEngine;
using Valley.Leaderboard;

namespace Valley.UI
{
    public class LeaderboardUI: MonoBehaviour
    {
        public void OpenLeaderboard()
        {
            if (GooglePlayLeaderboard.Instance == null)
            {
                Debug.LogWarning(
                    "[Leaderboard UI] GooglePlayLeaderboard instance not found."
                );

                return;
            }

            GooglePlayLeaderboard.Instance.ShowLeaderboard();
        }
    }
}