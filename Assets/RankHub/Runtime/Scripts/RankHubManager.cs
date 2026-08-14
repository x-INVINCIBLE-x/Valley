using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

namespace RankHub
{
    public class RankHubManager : MonoBehaviour
    {
        [Header("RankHub Configuration")]
        [Header("Your API Key. Create one at https://leaderboard-game.vercel.app")]
        // STEP 1: Create your leaderboard at:
        // https://leaderboard-game.vercel.app
        // STEP 2: Copy your API key
        // STEP 3: Paste it below
        [Header("Paste your API Key here (get it from the RankHub website)")]
        [SerializeField] private string apiKey;
        private string baseUrl = "https://leaderboard-game.vercel.app/api";
                
        // Singleton pattern
        public static RankHubManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Genera un player_id único usando GUID
        /// </summary>
        public string GeneratePlayerId()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Envía un score al leaderboard
        /// </summary>
        public void AddScore(string playerName, int score, string playerId, 
            System.Action<AddScoreResponse> onSuccess, 
            System.Action<string> onError)
        {
            StartCoroutine(AddScoreCoroutine(playerName, score, playerId, onSuccess, onError));
        }

        private IEnumerator AddScoreCoroutine(string playerName, int score, string playerId,
            System.Action<AddScoreResponse> onSuccess,
            System.Action<string> onError)
        {
            string url = $"{baseUrl}/add?key={apiKey}&player_id={playerId}&player={UnityWebRequest.EscapeURL(playerName)}&score={score}";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<AddScoreResponse>(request.downloadHandler.text);
                        if (response.success)
                        {
                            onSuccess?.Invoke(response);
                        }
                        else
                        {
                            onError?.Invoke("Unknown error");
                        }
                    }
                    catch
                    {
                        var error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                        onError?.Invoke(error.error);
                    }
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        /// <summary>
        /// Obtiene el leaderboard actual
        /// </summary>
        public void GetLeaderboard(int limit, int page, 
            System.Action<LeaderboardResponse> onSuccess,
            System.Action<string> onError)
        {
            StartCoroutine(GetLeaderboardCoroutine(limit, page, onSuccess, onError));
        }

        private IEnumerator GetLeaderboardCoroutine(int limit, int page,
            System.Action<LeaderboardResponse> onSuccess,
            System.Action<string> onError)
        {
            string url = $"{baseUrl}/top?key={apiKey}&limit={limit}&page={page}";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        /// <summary>
        /// Borra un score específico (solo planes de pago)
        /// </summary>
        public void DeleteScore(string playerId,
            System.Action<AddScoreResponse> onSuccess,
            System.Action<string> onError)
        {
            StartCoroutine(DeleteScoreCoroutine(playerId, onSuccess, onError));
        }

        private IEnumerator DeleteScoreCoroutine(string playerId,
            System.Action<AddScoreResponse> onSuccess,
            System.Action<string> onError)
        {
            string url = $"{baseUrl}/delete?key={apiKey}&player_id={playerId}";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AddScoreResponse>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }
    }
}