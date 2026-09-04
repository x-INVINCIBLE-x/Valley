using System;
using System.Text;
using UnityEngine;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

namespace Valley
{
    public class GooglePlaySaveManager : MonoBehaviour
    {
        public static GooglePlaySaveManager Instance { get; private set; }
        public static event Action CloudSaveReady;

        [SerializeField]
        private string saveFileName = "player_save";

        [SerializeField]
        private bool debugLogs = true;

        public bool IsReady { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoginManager.PlayerSignedIn += HandlePlayerSignedIn;
        }

        private void OnDestroy()
        {
            LoginManager.PlayerSignedIn -= HandlePlayerSignedIn;
        }

        private void HandlePlayerSignedIn()
        {
#if UNITY_ANDROID
            IsReady = true;

            Log(
                "Google Play Games authentication is ready."
            );

            CloudSaveReady?.Invoke();
#endif
        }

        // ==================================================
        // SAVE
        // ==================================================

        public void Save<T>(
            T data,
            Action<bool> onComplete = null)
        {
#if UNITY_ANDROID
            bool googleSignedIn =
                PlayGamesPlatform.Instance != null &&
                PlayGamesPlatform.Instance.IsAuthenticated();

            Log(
                $"Cloud save check - " +
                $"IsReady: {IsReady}, " +
                $"GoogleSignedIn: {googleSignedIn}"
            );

            if (!IsReady || !googleSignedIn)
            {
                LogWarning(
                    "Cannot save to Google Play. " +
                    "Google Play Games is not signed in."
                );

                onComplete?.Invoke(false);
                return;
            }

            if (data == null)
            {
                LogWarning(
                    "Cannot save null data."
                );

                onComplete?.Invoke(false);
                return;
            }

            string json =
                JsonUtility.ToJson(data);

            byte[] bytes =
                Encoding.UTF8.GetBytes(json);

            Log(
                $"Opening cloud save for writing. " +
                $"Bytes={bytes.Length}"
            );

            Log(
                $"Cloud JSON being written: {json}"
            );

            PlayGamesPlatform.Instance.SavedGame
                .OpenWithAutomaticConflictResolution(
                    saveFileName,
                    DataSource.ReadCacheOrNetwork,
                    ConflictResolutionStrategy.UseMostRecentlySaved,
                    (status, game) =>
                    {
                        if (status != SavedGameRequestStatus.Success)
                        {
                            LogError(
                                $"Failed to open cloud save: {status}"
                            );

                            onComplete?.Invoke(false);
                            return;
                        }

                        if (game == null)
                        {
                            LogError(
                                "Cloud save opened successfully " +
                                "but returned null metadata."
                            );

                            onComplete?.Invoke(false);
                            return;
                        }

                        SavedGameMetadataUpdate metadata =
                            new SavedGameMetadataUpdate.Builder()
                                .WithUpdatedDescription(
                                    $"Saved {DateTime.Now:G}")
                                .Build();

                        PlayGamesPlatform.Instance.SavedGame
                            .CommitUpdate(
                                game,
                                metadata,
                                bytes,
                                (saveStatus, savedGame) =>
                                {
                                    bool success =
                                        saveStatus ==
                                        SavedGameRequestStatus.Success;

                                    Log(
                                        $"Cloud save CommitUpdate result: " +
                                        $"Status={saveStatus}, " +
                                        $"Success={success}"
                                    );

                                    if (savedGame != null)
                                    {
                                        Log(
                                            $"Cloud save AFTER commit: " +
                                            $"Filename={savedGame.Filename}, " +
                                            $"Description={savedGame.Description}, " +
                                            $"LastModified={savedGame.LastModifiedTimestamp}"
                                        );
                                    }

                                    if (success)
                                    {
                                        Log(
                                            "Cloud save completed."
                                        );
                                    }
                                    else
                                    {
                                        LogError(
                                            $"Cloud save failed: {saveStatus}"
                                        );
                                    }

                                    onComplete?.Invoke(success);
                                }
                            );
                    }
                );
#else
            onComplete?.Invoke(false);
#endif
        }

        // ==================================================
        // LOAD
        // ==================================================

        public void Load<T>(Action<T> onComplete)
        {
#if UNITY_ANDROID
            if (!IsReady ||
                PlayGamesPlatform.Instance == null ||
                !PlayGamesPlatform.Instance.IsAuthenticated())
            {
                LogWarning(
                    "Cannot load from Google Play. " +
                    "Player is not signed in."
                );

                onComplete?.Invoke(default);
                return;
            }

            Log(
                $"Opening cloud save for NETWORK READ. " +
                $"File={saveFileName}"
            );

            PlayGamesPlatform.Instance.SavedGame
                .OpenWithAutomaticConflictResolution(
                    saveFileName,
                    DataSource.ReadNetworkOnly,
                    ConflictResolutionStrategy.UseMostRecentlySaved,
                    (status, game) =>
                    {
                        if (status != SavedGameRequestStatus.Success)
                        {
                            LogWarning(
                                $"Cloud save could not be opened: {status}"
                            );

                            onComplete?.Invoke(default);
                            return;
                        }

                        if (game == null)
                        {
                            LogWarning(
                                "Cloud save opened successfully " +
                                "but metadata is null."
                            );

                            onComplete?.Invoke(default);
                            return;
                        }

                        Log(
                            $"Cloud save opened successfully. " +
                            $"Filename={game.Filename}, " +
                            $"Description={game.Description}, " +
                            $"LastModified={game.LastModifiedTimestamp}"
                        );

                        PlayGamesPlatform.Instance.SavedGame
                            .ReadBinaryData(
                                game,
                                (readStatus, data) =>
                                {
                                    if (readStatus != SavedGameRequestStatus.Success)
                                    {
                                        LogWarning(
                                            $"Cloud save could not be read: " +
                                            $"{readStatus}"
                                        );

                                        onComplete?.Invoke(default);
                                        return;
                                    }

                                    if (data == null ||
                                        data.Length == 0)
                                    {
                                        Log(
                                            "No cloud save data exists."
                                        );

                                        onComplete?.Invoke(default);
                                        return;
                                    }

                                    Log(
                                        $"Cloud save binary data received. " +
                                        $"Bytes={data.Length}"
                                    );

                                    try
                                    {
                                        string json =
                                            Encoding.UTF8.GetString(data);

                                        Log(
                                            $"Cloud JSON: {json}"
                                        );

                                        T saveData =
                                            JsonUtility.FromJson<T>(
                                                json
                                            );

                                        if (saveData == null)
                                        {
                                            LogWarning(
                                                "Cloud save JSON " +
                                                "deserialized to null."
                                            );

                                            onComplete?.Invoke(default);
                                            return;
                                        }

                                        Log(
                                            "Cloud save loaded successfully."
                                        );

                                        onComplete?.Invoke(
                                            saveData
                                        );
                                    }
                                    catch (Exception ex)
                                    {
                                        LogError(
                                            "Cloud save deserialization " +
                                            $"failed: {ex.Message}"
                                        );

                                        onComplete?.Invoke(default);
                                    }
                                }
                            );
                    }
                );
#else
            onComplete?.Invoke(default);
#endif
        }

        // ==================================================
        // LOGGING
        // ==================================================

        private void Log(string message)
        {
            if (debugLogs)
            {
                Debug.Log(
                    $"[GooglePlaySaveManager] {message}"
                );
            }
        }

        private void LogWarning(string message)
        {
            if (debugLogs)
            {
                Debug.LogWarning(
                    $"[GooglePlaySaveManager] {message}"
                );
            }
        }

        private void LogError(string message)
        {
            Debug.LogError(
                $"[GooglePlaySaveManager] {message}"
            );
        }
    }
}
