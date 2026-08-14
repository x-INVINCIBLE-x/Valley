using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace RankHub
{
    /// <summary>
    /// Main UI controller for RankHub leaderboard integration.
    /// - Add Score: Only increases local score (simulates gameplay)
    /// - Submit: Only sends current score to API
    /// - Edit Name: Changes player name (requires submit to save)
    /// </summary>
    public class RankHub_Game : MonoBehaviour
    {
        [Header("Player HUD - Always Visible")]
        [Tooltip("Shows player name (always visible)")]
        [SerializeField] private TMP_Text nameHUD;
        
        [Tooltip("Shows player score (always visible)")]
        [SerializeField] private TMP_Text scoreHUD;
        
        [Header("Input Fields")]
        [Tooltip("Input field for player name (only visible when editing)")]
        [SerializeField] private TMP_InputField playerNameInput;
        
        [Header("Score Button")]
        [Tooltip("Button to add +10 score LOCALLY (simulates gaining points)")]
        [SerializeField] private Button addScoreButton;
        
        [Header("Display")]
        [Tooltip("Container where leaderboard entries will be instantiated")]
        [SerializeField] private Transform leaderboardContainer;
        
        [Tooltip("Prefab for each leaderboard entry (must have 3 TMP_Text: Rank, Player, Score)")]
        [SerializeField] private GameObject scoreEntryPrefab;
        
        [Tooltip("Shows current status (loading, page info, etc.)")]
        [SerializeField] private TMP_Text statusText;
        
        [Tooltip("Shows feedback messages after submitting scores")]
        [SerializeField] private TMP_Text feedbackText;
        
        [Header("Buttons")]
        [SerializeField] private Button submitButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button editNameButton;
        
        [Header("Settings")]
        [Tooltip("Number of scores to display per page")]
        [SerializeField] private int scoresPerPage = 10;
        
        [Header("UI Container")]
        [Tooltip("Container that holds the name input field (shown during editing)")]
        [SerializeField] private GameObject nameInputContainer;
        
        // =========================================================
        // PRIVATE VARIABLES
        // =========================================================
        private int currentPage = 1;
        private bool isSubmitting = false;
        private bool isEditingName = false;
        
        private string playerId;
        private string originalNameBeforeEdit;
        private int currentPlayerScore = 0;      // Local score (can be higher than submitted)
        private int lastSubmittedScore = 0;       // Last score successfully submitted
        private bool hasSavedScore = false;
        
        // PlayerPrefs keys
        private const string PLAYER_ID_KEY = "RankHub_PlayerID";
        private const string PLAYER_NAME_KEY = "RankHub_PlayerName";
        private const string PLAYER_SCORE_KEY = "RankHub_PlayerScore";
        
        // =========================================================
        // INITIALIZATION
        // =========================================================
        private IEnumerator Start()
        {
            yield return new WaitUntil(() => RankHubManager.Instance != null);
            
            LoadOrCreatePlayerId();
            LoadSavedData();
            SetupButtonListeners();
            SetupDefaultName();
            UpdateHUDs();
            UpdateUIVisibility();
            
            // Initial leaderboard load
            RefreshLeaderboard();
        }
        
        private void LoadSavedData()
        {
            // Load saved score (last submitted score)
            lastSubmittedScore = PlayerPrefs.GetInt(PLAYER_SCORE_KEY, 0);
            currentPlayerScore = lastSubmittedScore; // Start with submitted score
            hasSavedScore = lastSubmittedScore > 0;
        }
        
        private void SetupButtonListeners()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitScore);
                
            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshLeaderboard);
                
            if (nextPageButton != null)
                nextPageButton.onClick.AddListener(NextPage);
                
            if (prevPageButton != null)
                prevPageButton.onClick.AddListener(PrevPage);
                
            if (editNameButton != null)
                editNameButton.onClick.AddListener(OnEditName);
                
            if (addScoreButton != null)
                addScoreButton.onClick.AddListener(OnAddScoreLocally);
        }
        
        private void SetupDefaultName()
        {
            if (!PlayerPrefs.HasKey(PLAYER_NAME_KEY))
            {
                string defaultName = "Player" + Random.Range(10000, 99999).ToString();
                PlayerPrefs.SetString(PLAYER_NAME_KEY, defaultName);
                PlayerPrefs.Save();
            }
            
            playerNameInput.text = PlayerPrefs.GetString(PLAYER_NAME_KEY);
        }
        
        private void LoadOrCreatePlayerId()
        {
            if (PlayerPrefs.HasKey(PLAYER_ID_KEY))
            {
                playerId = PlayerPrefs.GetString(PLAYER_ID_KEY);
                Debug.Log($"[RankHub] Player ID loaded: {playerId}");
            }
            else
            {
                playerId = RankHubManager.Instance.GeneratePlayerId();
                PlayerPrefs.SetString(PLAYER_ID_KEY, playerId);
                PlayerPrefs.Save();
                Debug.Log($"[RankHub] New Player ID created: {playerId}");
            }
        }
        
        private void UpdateHUDs()
        {
            if (nameHUD != null)
                nameHUD.text = PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player");
                
            if (scoreHUD != null)
                scoreHUD.text = currentPlayerScore.ToString();
        }
        
        private void UpdateUIVisibility()
        {
            // HUDs always visible
            if (nameHUD != null) nameHUD.gameObject.SetActive(true);
            if (scoreHUD != null) scoreHUD.gameObject.SetActive(true);
            
            // Name input container only during editing
            if (nameInputContainer != null)
                nameInputContainer.SetActive(isEditingName);
        }
        
        // =========================================================
        // BUTTON HANDLERS
        // =========================================================
        
        /// <summary>
        /// ONLY adds score locally - does NOT submit to API
        /// Simulates gameplay - player earns points
        /// </summary>
        private void OnAddScoreLocally()
        {
            currentPlayerScore += 10;
            UpdateHUDs();
            SetFeedback($"Score +10! Current: {currentPlayerScore} (Submit to save)", Color.white);
        }
        
        private void OnEditName()
        {
            isEditingName = true;
            originalNameBeforeEdit = playerNameInput.text;
            
            UpdateUIVisibility();
            playerNameInput.Select();
            playerNameInput.ActivateInputField();
            
            SetFeedback("Edit your name and click SUBMIT to save", Color.white);
        }
        
        /// <summary>
        /// ONLY submits current score to API
        /// Does NOT modify local score
        /// </summary>
        private void OnSubmitScore()
        {
            if (isSubmitting)
            {
                SetFeedback("Please wait...", Color.yellow);
                return;
            }
            
            // Get current player name (from input if editing, otherwise saved)
            string playerName = GetCurrentPlayerName();
            
            isSubmitting = true;
            SetStatus("Submitting...", Color.white);
            ClearFeedback();
            
            RankHubManager.Instance.AddScore(
                playerName, 
                currentPlayerScore, 
                playerId,
                OnSubmitSuccess,
                (error) => OnSubmitError(error, playerName)
            );
        }
        
        private string GetCurrentPlayerName()
        {
            if (isEditingName && !string.IsNullOrEmpty(playerNameInput.text))
            {
                return playerNameInput.text;
            }
            return PlayerPrefs.GetString(PLAYER_NAME_KEY, "Player");
        }
        
        private void RefreshLeaderboard()
        {
            SetStatus("Loading...", Color.white);
            ClearFeedback();
            
            RankHubManager.Instance.GetLeaderboard(
                scoresPerPage, 
                currentPage,
                OnLeaderboardLoaded,
                OnError
            );
        }
        
        // =========================================================
        // API RESPONSE HANDLERS
        // =========================================================
        
        private void OnSubmitSuccess(AddScoreResponse response)
        {
            isSubmitting = false;
            
            if (response.success)
            {
                // =========================================================
                // HANDLE NAME CHANGE
                // =========================================================
                if (isEditingName)
                {
                    PlayerPrefs.SetString(PLAYER_NAME_KEY, playerNameInput.text);
                    PlayerPrefs.Save();
                    isEditingName = false;
                    
                    if (nameHUD != null)
                        nameHUD.text = playerNameInput.text;
                    
                    UpdateUIVisibility();
                }
                
                // =========================================================
                // HANDLE SCORE SUBMISSION FEEDBACK
                // =========================================================
                if (response.message.Contains("not updated"))
                {
                    // Score was lower than best - didn't enter leaderboard
                    SetFeedback($"Score not improved. Your best is still {lastSubmittedScore}. Keep playing!", Color.yellow);
                }
                else if (response.message.Contains("replaced"))
                {
                    // New score entered leaderboard and replaced someone
                    lastSubmittedScore = currentPlayerScore;
                    PlayerPrefs.SetInt(PLAYER_SCORE_KEY, lastSubmittedScore);
                    PlayerPrefs.Save();
                    
                    SetFeedback($"NEW RECORD! You beat {response.replaced_player}'s score of {response.replaced_score}!", new Color(1, 0.5f, 0, 1));
                }
                else if (response.message.Contains("updated"))
                {
                    // New personal best
                    lastSubmittedScore = currentPlayerScore;
                    PlayerPrefs.SetInt(PLAYER_SCORE_KEY, lastSubmittedScore);
                    PlayerPrefs.Save();
                    
                    SetFeedback($"New personal best: {currentPlayerScore} pts!", Color.green);
                }
                else if (response.message.Contains("added"))
                {
                    // First score submission
                    lastSubmittedScore = currentPlayerScore;
                    PlayerPrefs.SetInt(PLAYER_SCORE_KEY, lastSubmittedScore);
                    PlayerPrefs.Save();
                    
                    SetFeedback($"First score recorded: {currentPlayerScore} pts!", Color.green);
                }
                
                RefreshLeaderboard();
            }
        }
        
        private void OnLeaderboardLoaded(LeaderboardResponse response)
        {
            // Clear existing entries
            foreach (Transform child in leaderboardContainer)
            {
                Destroy(child.gameObject);
            }
            
            int playerRank = 0;
            int playerScore = 0;
            int startRank = (currentPage - 1) * scoresPerPage + 1;
            
            // Create new entries
            for (int i = 0; i < response.scores.Length; i++)
            {
                var score = response.scores[i];
                int rank = startRank + i;
                
                var entry = Instantiate(scoreEntryPrefab, leaderboardContainer);
                var texts = entry.GetComponentsInChildren<TMP_Text>();
                
                if (texts.Length >= 3)
                {
                    texts[0].text = rank.ToString();
                    texts[1].text = score.player;
                    texts[2].text = score.score.ToString();
                }
                
                // Track player's data
                if (score.player_id == playerId)
                {
                    playerRank = rank;
                    playerScore = score.score;
                    
                    // Update last submitted score if leaderboard shows higher
                    if (playerScore > lastSubmittedScore)
                    {
                        lastSubmittedScore = playerScore;
                        PlayerPrefs.SetInt(PLAYER_SCORE_KEY, lastSubmittedScore);
                        PlayerPrefs.Save();
                    }
                    
                    // Highlight player's entry
                    var image = entry.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = new Color(1, 1, 0, 0.2f);
                    }
                }
            }
            
            // Update pagination info
            int totalPages = Mathf.CeilToInt((float)response.total_scores / scoresPerPage);
            
            if (playerRank > 0)
            {
                SetStatus($"Page {currentPage} of {totalPages} · You're #{playerRank} with {playerScore} pts · Total: {response.total_scores} scores", Color.white);
            }
            else
            {
                SetStatus($"Page {currentPage} of {totalPages} · Total: {response.total_scores} scores", Color.white);
            }
            
            // Update pagination buttons
            if (prevPageButton != null)
                prevPageButton.interactable = currentPage > 1;
                
            if (nextPageButton != null)
                nextPageButton.interactable = currentPage < totalPages;
        }
        
        // =========================================================
        // PAGINATION
        // =========================================================
        private void NextPage()
        {
            currentPage++;
            RefreshLeaderboard();
            ClearFeedback();
        }
        
        private void PrevPage()
        {
            currentPage--;
            RefreshLeaderboard();
            ClearFeedback();
        }
        
        // =========================================================
        // ERROR HANDLING
        // =========================================================
        
        private void OnError(string error)
        {
            isSubmitting = false;
            
            if (error.Contains("403") || error.Contains("Forbidden"))
            {
                SetFeedback("Leaderboard full. Keep playing to beat the lowest score!", Color.yellow);
            }
            else
            {
                SetFeedback($"Error: {error}", Color.red);
            }
            
            SetStatus("Error loading", Color.red);
        }
        
        private void OnSubmitError(string error, string attemptedName)
        {
            isSubmitting = false;
            
            if (error.Contains("403") || error.Contains("Forbidden"))
            {
                SetFeedback("Leaderboard full. Your score isn't high enough to enter the top.", Color.yellow);
            }
            else
            {
                SetFeedback($"Error: {error}", Color.red);
            }
            
            // Restore name if edit failed
            if (isEditingName)
            {
                playerNameInput.text = originalNameBeforeEdit;
                isEditingName = false;
                UpdateUIVisibility();
                SetFeedback("Name restored to original", Color.yellow);
            }
            
            SetStatus("Error", Color.red);
        }
        
        // =========================================================
        // UI FEEDBACK HELPERS
        // =========================================================
        
        private void SetStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }
        
        private void SetFeedback(string message, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
            }
            else
            {
                Debug.Log($"[RankHub] {message}");
            }
        }
        
        private void ClearFeedback()
        {
            if (feedbackText != null)
            {
                feedbackText.text = "";
            }
        }
    }
}