using UnityEngine;

namespace RankHub
{
    public class RankHubExample : MonoBehaviour
    {
        public int score = 100;

        public void SendScore()
        {
            // This works AS LONG AS:
            // 1. The RankHubManager is in the scene
            // 2. Have the correct API Key configured
            // 3. The player_id is generated correctly
            RankHub.RankHubManager.Instance.AddScore(
                "Player",
                score,
                System.Guid.NewGuid().ToString(),
                (res) => Debug.Log("Score sent"),
                (err) => Debug.LogError(err)
            );
        }
    }
}

