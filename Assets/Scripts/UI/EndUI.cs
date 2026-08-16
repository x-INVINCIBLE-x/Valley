using TMPro;
using UnityEngine;
using Valley.Scoring;

namespace Valley.UI
{
    /// <summary>
    /// End-of-run screen. Displays RunPeak (the highest score/distance reached during this
    /// run) rather than Current, since Current can dip from backward movement and would
    /// otherwise under-report what the player actually achieved. Caches references to
    /// PlayerScoreData's RunPeak/Best ScoreRecord instances once; since they're mutated in
    /// place, the cached references always reflect the latest values.
    /// </summary>
    [AddComponentMenu("Valley/UI/End UI")]
    public class EndUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlayerScoreData scoreData;

        [Header("Text Fields")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text highDistanceText;

        [Header("Optional")]
        [Tooltip("Enabled when this run's peak score/distance is tied with the current best.")]
        [SerializeField] private GameObject newHighScoreBadge;

        private ScoreRecord _runPeak;
        private ScoreRecord _best;

        private void Awake()
        {
            _runPeak = scoreData.RunPeak;
            _best = scoreData.Best;
        }

        private void OnEnable()
        {
            if (scoreText != null) scoreText.text = FormatScore(_runPeak.Score);
            if (distanceText != null) distanceText.text = FormatDistance(_runPeak.Distance);
            if (highScoreText != null) highScoreText.text = FormatScore(_best.Score);
            if (highDistanceText != null) highDistanceText.text = FormatDistance(_best.Distance);

            if (newHighScoreBadge != null)
            {
                bool isNewBest = _runPeak.Score > 0f &&
                                  (Mathf.Approximately(_runPeak.Score, _best.Score) ||
                                   Mathf.Approximately(_runPeak.Distance, _best.Distance));
                newHighScoreBadge.SetActive(isNewBest);
            }
        }

        private static string FormatScore(float score) => Mathf.FloorToInt(score).ToString();
        private static string FormatDistance(float distance) => $"{Mathf.FloorToInt(distance)}m";
    }
}