using System;
using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

namespace Valley.Leaderboard
{
    public class GooglePlayLeaderboard : MonoBehaviour
    {
        public static GooglePlayLeaderboard Instance { get; private set; }

        [Header("Leaderboard")]
        [Tooltip("Google Play Games leaderboard ID.")]
        [SerializeField] private string leaderboardId;

        [Header("Authentication")]
        [SerializeField] private bool authenticateOnStart = true;

        private bool m_Authenticated;
        private bool m_AuthenticationInProgress;

        private long m_PendingScore = -1;
        private bool m_HasPendingScore;

        public bool IsAuthenticated => m_Authenticated;

        public string LeaderboardId => leaderboardId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            PlayGamesPlatform.Activate();

            if (authenticateOnStart)
            {
                Authenticate();
            }
        }

        public void Authenticate(Action<bool> onComplete = null)
        {
            if (m_Authenticated)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (m_AuthenticationInProgress)
            {
                return;
            }

            m_AuthenticationInProgress = true;

            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                m_AuthenticationInProgress = false;

                if (status == SignInStatus.Success)
                {
                    m_Authenticated = true;

                    Debug.Log("[Google Play Games] Authentication successful.");

                    SubmitPendingScore();

                    onComplete?.Invoke(true);
                }
                else
                {
                    m_Authenticated = false;

                    Debug.LogWarning(
                        "[Google Play Games] Authentication failed: " +
                        status
                    );

                    onComplete?.Invoke(false);
                }
            });
        }

        public void SubmitScore(long score)
        {
            if (score < 0)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] Score cannot be negative."
                );

                return;
            }

            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogError(
                    "[Google Play Leaderboard] Leaderboard ID is empty."
                );

                return;
            }

            if (!m_Authenticated)
            {
                m_PendingScore = score;
                m_HasPendingScore = true;

                Debug.Log(
                    "[Google Play Leaderboard] Player is not authenticated. " +
                    "Score queued."
                );

                Authenticate();

                return;
            }

            SubmitAuthenticatedScore(score);
        }

        private void SubmitAuthenticatedScore(long score)
        {
            PlayGamesPlatform.Instance.ReportScore(
                score,
                leaderboardId,
                success =>
                {
                    if (success)
                    {
                        Debug.Log(
                            "[Google Play Leaderboard] Score submitted: " +
                            score
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Google Play Leaderboard] Failed to submit score: " +
                            score
                        );
                    }
                }
            );
        }

        private void SubmitPendingScore()
        {
            if (!m_HasPendingScore)
            {
                return;
            }

            long score = m_PendingScore;

            m_PendingScore = -1;
            m_HasPendingScore = false;

            SubmitAuthenticatedScore(score);
        }

        public void ShowLeaderboard()
        {
            if (!m_Authenticated)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] Player is not authenticated. " +
                    "Attempting authentication first."
                );

                Authenticate(success =>
                {
                    if (success)
                    {
                        ShowLeaderboardInternal();
                    }
                });

                return;
            }

            ShowLeaderboardInternal();
        }

        private void ShowLeaderboardInternal()
        {
            PlayGamesPlatform.Instance.ShowLeaderboardUI();
        }

        public void LoadTopScores(
            int rowCount,
            Action<LeaderboardScoreData> onComplete)
        {
            if (!m_Authenticated)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] Cannot load scores. " +
                    "Player is not authenticated."
                );

                onComplete?.Invoke(null);
                return;
            }

            PlayGamesPlatform.Instance.LoadScores(
                leaderboardId,
                LeaderboardStart.TopScores,
                rowCount,
                LeaderboardCollection.Public,
                LeaderboardTimeSpan.AllTime,
                data =>
                {
                    if (data.Status == ResponseStatus.Success)
                    {
                        Debug.Log(
                            "[Google Play Leaderboard] Loaded top scores."
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Google Play Leaderboard] Failed to load scores. " +
                            "Status: " + data.Status
                        );
                    }

                    onComplete?.Invoke(data);
                }
            );
        }

        public void LoadPlayerCenteredScores(
            int rowCount,
            Action<LeaderboardScoreData> onComplete)
        {
            if (!m_Authenticated)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] Cannot load player scores. " +
                    "Player is not authenticated."
                );

                onComplete?.Invoke(null);
                return;
            }

            PlayGamesPlatform.Instance.LoadScores(
                leaderboardId,
                LeaderboardStart.PlayerCentered,
                rowCount,
                LeaderboardCollection.Public,
                LeaderboardTimeSpan.AllTime,
                data =>
                {
                    if (data.Status == ResponseStatus.Success)
                    {
                        Debug.Log(
                            "[Google Play Leaderboard] Loaded player-centered scores."
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Google Play Leaderboard] Failed to load " +
                            "player-centered scores. Status: " +
                            data.Status
                        );
                    }

                    onComplete?.Invoke(data);
                }
            );
        }

        public void LoadPlayerScore(
            Action<LeaderboardScoreData> onComplete)
        {
            LoadPlayerCenteredScores(1, onComplete);
        }
    }
}