using System;
using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

namespace Valley.Leaderboard
{
    public class GooglePlayLeaderboard : MonoBehaviour
    {
        public static GooglePlayLeaderboard Instance { get; private set; }

        // ==================================================
        // LEADERBOARD
        // ==================================================

        [Header("Leaderboards")]

        [Tooltip("Google Play Games leaderboard ID for high score.")]
        [SerializeField] private string leaderboardId;

        [Tooltip("Google Play Games leaderboard ID for high distance.")]
        [SerializeField] private string distanceLeaderboardId;

        public string LeaderboardId => leaderboardId;

        public string DistanceLeaderboardId =>
            distanceLeaderboardId;


        // ==================================================
        // AUTHENTICATION
        // ==================================================

        [Header("Authentication")]

        [SerializeField] private bool authenticateOnStart = true;

        private bool m_Authenticated;
        private bool m_AuthenticationInProgress;

        public bool IsAuthenticated => m_Authenticated;


        // ==================================================
        // PENDING SUBMISSIONS
        // ==================================================

        private long m_PendingScore = -1;
        private bool m_HasPendingScore;

        private long m_PendingDistance = -1;
        private bool m_HasPendingDistance;


        // ==================================================
        // UNITY
        // ==================================================

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


        // ==================================================
        // AUTHENTICATION
        // ==================================================

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

                    Debug.Log(
                        "[Google Play Games] " +
                        "Authentication successful."
                    );

                    SubmitPendingScores();

                    onComplete?.Invoke(true);
                }
                else
                {
                    m_Authenticated = false;

                    Debug.LogWarning(
                        "[Google Play Games] " +
                        "Authentication failed: " +
                        status
                    );

                    onComplete?.Invoke(false);
                }
            });
        }


        // ==================================================
        // SCORE SUBMISSION
        // ==================================================

        /// <summary>
        /// Submits the player's high score.
        /// </summary>
        public void SubmitScore(long score)
        {
            if (score < 0)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] " +
                    "Score cannot be negative."
                );

                return;
            }

            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogError(
                    "[Google Play Leaderboard] " +
                    "Score leaderboard ID is empty."
                );

                return;
            }

            if (!m_Authenticated)
            {
                m_PendingScore = score;
                m_HasPendingScore = true;

                Debug.Log(
                    "[Google Play Leaderboard] " +
                    "Player is not authenticated. " +
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
                            "[Google Play Leaderboard] " +
                            "High score submitted: " +
                            score
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Google Play Leaderboard] " +
                            "Failed to submit high score: " +
                            score
                        );
                    }
                }
            );
        }


        // ==================================================
        // DISTANCE SUBMISSION
        // ==================================================

        /// <summary>
        /// Submits the player's high distance.
        /// </summary>
        public void SubmitDistance(long distance)
        {
            if (distance < 0)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] " +
                    "Distance cannot be negative."
                );

                return;
            }

            if (string.IsNullOrEmpty(distanceLeaderboardId))
            {
                Debug.LogError(
                    "[Google Play Leaderboard] " +
                    "Distance leaderboard ID is empty."
                );

                return;
            }

            if (!m_Authenticated)
            {
                m_PendingDistance = distance;
                m_HasPendingDistance = true;

                Debug.Log(
                    "[Google Play Leaderboard] " +
                    "Player is not authenticated. " +
                    "Distance queued."
                );

                Authenticate();

                return;
            }

            SubmitAuthenticatedDistance(distance);
        }

        private void SubmitAuthenticatedDistance(long distance)
        {
            PlayGamesPlatform.Instance.ReportScore(
                distance,
                distanceLeaderboardId,
                success =>
                {
                    if (success)
                    {
                        Debug.Log(
                            "[Google Play Leaderboard] " +
                            "High distance submitted: " +
                            distance
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Google Play Leaderboard] " +
                            "Failed to submit high distance: " +
                            distance
                        );
                    }
                }
            );
        }


        // ==================================================
        // PENDING SUBMISSIONS
        // ==================================================

        private void SubmitPendingScores()
        {
            if (m_HasPendingScore)
            {
                long score = m_PendingScore;

                m_PendingScore = -1;
                m_HasPendingScore = false;

                SubmitAuthenticatedScore(score);
            }

            if (m_HasPendingDistance)
            {
                long distance = m_PendingDistance;

                m_PendingDistance = -1;
                m_HasPendingDistance = false;

                SubmitAuthenticatedDistance(distance);
            }
        }


        // ==================================================
        // SHOW SCORE LEADERBOARD
        // ==================================================

        /// <summary>
        /// Shows the Google Play Games leaderboard UI.
        /// </summary>
        public void ShowLeaderboard()
        {
            if (!m_Authenticated)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] " +
                    "Player is not authenticated. " +
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


        // ==================================================
        // LOAD TOP SCORE LEADERBOARD
        // ==================================================

        /// <summary>
        /// Loads the top score leaderboard.
        /// </summary>
        public void LoadTopScores(
            int rowCount,
            Action<LeaderboardScoreData> onComplete)
        {
            if (!m_Authenticated)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] " +
                    "Cannot load scores. " +
                    "Player is not authenticated."
                );

                onComplete?.Invoke(null);
                return;
            }

            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogError(
                    "[Google Play Leaderboard] " +
                    "Score leaderboard ID is empty."
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
                            "[Google Play Leaderboard] " +
                            "Loaded top scores."
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Google Play Leaderboard] " +
                            "Failed to load scores. Status: " +
                            data.Status
                        );
                    }

                    onComplete?.Invoke(data);
                }
            );
        }


        // ==================================================
        // LOAD PLAYER-CENTERED SCORE LEADERBOARD
        // ==================================================

        /// <summary>
        /// Loads scores around the current player.
        /// </summary>
        public void LoadPlayerCenteredScores(
            int rowCount,
            Action<LeaderboardScoreData> onComplete)
        {
            if (!m_Authenticated)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] " +
                    "Cannot load player scores. " +
                    "Player is not authenticated."
                );

                onComplete?.Invoke(null);
                return;
            }

            if (string.IsNullOrEmpty(leaderboardId))
            {
                Debug.LogError(
                    "[Google Play Leaderboard] " +
                    "Score leaderboard ID is empty."
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
                            "[Google Play Leaderboard] " +
                            "Loaded player-centered scores."
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Google Play Leaderboard] " +
                            "Failed to load player-centered " +
                            "scores. Status: " +
                            data.Status
                        );
                    }

                    onComplete?.Invoke(data);
                }
            );
        }


        // ==================================================
        // LOAD PLAYER'S HIGH SCORE
        // ==================================================

        /// <summary>
        /// Loads the player's score from the score leaderboard.
        /// </summary>
        public void LoadPlayerScore(
            Action<LeaderboardScoreData> onComplete)
        {
            LoadPlayerCenteredScores(
                1,
                onComplete
            );
        }


        // ==================================================
        // LOAD TOP DISTANCE
        // ==================================================

        /// <summary>
        /// Loads the top distance leaderboard.
        /// </summary>
        public void LoadTopDistances(
            int rowCount,
            Action<LeaderboardScoreData> onComplete)
        {
            if (!m_Authenticated)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] " +
                    "Cannot load distances. " +
                    "Player is not authenticated."
                );

                onComplete?.Invoke(null);
                return;
            }

            if (string.IsNullOrEmpty(distanceLeaderboardId))
            {
                Debug.LogError(
                    "[Google Play Leaderboard] " +
                    "Distance leaderboard ID is empty."
                );

                onComplete?.Invoke(null);
                return;
            }

            PlayGamesPlatform.Instance.LoadScores(
                distanceLeaderboardId,
                LeaderboardStart.TopScores,
                rowCount,
                LeaderboardCollection.Public,
                LeaderboardTimeSpan.AllTime,
                data =>
                {
                    if (data.Status == ResponseStatus.Success)
                    {
                        Debug.Log(
                            "[Google Play Leaderboard] " +
                            "Loaded top distances."
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Google Play Leaderboard] " +
                            "Failed to load distances. Status: " +
                            data.Status
                        );
                    }

                    onComplete?.Invoke(data);
                }
            );
        }


        // ==================================================
        // LOAD PLAYER-CENTERED DISTANCE
        // ==================================================

        /// <summary>
        /// Loads distance scores around the current player.
        /// </summary>
        public void LoadPlayerCenteredDistances(
            int rowCount,
            Action<LeaderboardScoreData> onComplete)
        {
            if (!m_Authenticated)
            {
                Debug.LogWarning(
                    "[Google Play Leaderboard] " +
                    "Cannot load player distances. " +
                    "Player is not authenticated."
                );

                onComplete?.Invoke(null);
                return;
            }

            if (string.IsNullOrEmpty(distanceLeaderboardId))
            {
                Debug.LogError(
                    "[Google Play Leaderboard] " +
                    "Distance leaderboard ID is empty."
                );

                onComplete?.Invoke(null);
                return;
            }

            PlayGamesPlatform.Instance.LoadScores(
                distanceLeaderboardId,
                LeaderboardStart.PlayerCentered,
                rowCount,
                LeaderboardCollection.Public,
                LeaderboardTimeSpan.AllTime,
                data =>
                {
                    if (data.Status == ResponseStatus.Success)
                    {
                        Debug.Log(
                            "[Google Play Leaderboard] " +
                            "Loaded player-centered distances."
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Google Play Leaderboard] " +
                            "Failed to load player-centered " +
                            "distances. Status: " +
                            data.Status
                        );
                    }

                    onComplete?.Invoke(data);
                }
            );
        }


        // ==================================================
        // LOAD PLAYER'S HIGH DISTANCE
        // ==================================================

        /// <summary>
        /// Loads the player's high distance.
        /// </summary>
        public void LoadPlayerDistance(
            Action<LeaderboardScoreData> onComplete)
        {
            LoadPlayerCenteredDistances(
                1,
                onComplete
            );
        }
    }
}