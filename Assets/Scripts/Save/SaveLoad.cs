using Esper.ESave;
using System;
using UnityEngine;
using Valley;
using Valley.Economy;
using Valley.Scoring;
using Valley.Theming;

[RequireComponent(typeof(SaveFileSetup))]
public class SaveLoad : MonoBehaviour
{
    [SerializeField]
    private PlayerScoreData playerScoreData;

    private const string CurrencyDataKey = "Currency";
    private const string CurrentThemeDataKey = "CurrentTheme";
    private const string BoughtThemesDataKey = "BoughtThemes";

    private const string HighScoreDataKey = "HighScore";
    private const string HighDistanceDataKey = "HighDistance";

    private const string TemporaryThemeIdDataKey = "TemporaryTheme_Id";
    private const string TemporaryThemeExpiryDataKey = "TemporaryTheme_Expiry";
    private const string TemporaryThemePreviousIdDataKey = "TemporaryTheme_PreviousId";

    private SaveFile saveFile;

    private bool m_LoadCompleted;
    private bool m_CloudLoadStarted;

    private bool m_CloudSaveInProgress;
    private CloudSaveData m_PendingCloudSave;
    private Action<bool> m_PendingCloudSaveCallback;

    [Serializable]
    private class CloudSaveData
    {
        public int currency;
        public string boughtThemes;
        public float highScore;
        public float highDistance;
    }

    private void Awake()
    {
        SaveFileSetup setup = GetComponent<SaveFileSetup>();
        saveFile = setup.GetSaveFile();

        if (playerScoreData == null)
        {
            Debug.LogError("SaveLoad: PlayerScoreData is missing.");
        }
    }

    private void OnEnable()
    {
        GooglePlaySaveManager.CloudSaveReady += HandleCloudSaveReady;

        if (GooglePlaySaveManager.Instance != null &&
            GooglePlaySaveManager.Instance.IsReady)
        {
            HandleCloudSaveReady();
        }
    }

    private void OnDisable()
    {
        GooglePlaySaveManager.CloudSaveReady -= HandleCloudSaveReady;
    }

    // ==================================================
    // SAVE
    // ==================================================

    public void SaveGame()
    {
        if (saveFile == null)
        {
            Debug.LogError("SaveLoad: SaveFile is not initialized.");
            return;
        }

        SaveCurrency();
        SaveThemes();
        SaveTemporaryTheme();
        SaveHighScore();

        saveFile.Save();

        Debug.Log("Game saved locally.");
    }

    public void SaveGameToCloud(Action<bool> onComplete = null)
    {
        if (saveFile == null)
        {
            Debug.LogError(
                "SaveLoad: SaveFile is not initialized."
            );

            onComplete?.Invoke(false);
            return;
        }

        if (!m_LoadCompleted)
        {
            Debug.LogWarning(
                "SaveLoad: Initial cloud load has not completed. " +
                "Cloud save skipped."
            );

            onComplete?.Invoke(false);
            return;
        }

        if (GooglePlaySaveManager.Instance == null)
        {
            Debug.LogWarning(
                "SaveLoad: GooglePlaySaveManager is missing."
            );

            onComplete?.Invoke(false);
            return;
        }

        if (!GooglePlaySaveManager.Instance.IsReady)
        {
            Debug.LogWarning(
                "SaveLoad: Google Play cloud save is not ready."
            );

            onComplete?.Invoke(false);
            return;
        }

        SaveGame();

        CloudSaveData data =
            CreateCloudSaveData();

        Debug.Log(
            $"[Score Cloud Save Request] " +
            $"HighScore={data.highScore}, " +
            $"HighDistance={data.highDistance}"
        );

        if (m_CloudSaveInProgress)
        {
            m_PendingCloudSave = data;
            m_PendingCloudSaveCallback = onComplete;

            Debug.Log(
                "[Cloud Save] Save already in progress. " +
                "Latest state queued."
            );

            return;
        }

        StartCloudSave(
            data,
            onComplete
        );
    }

    private void StartCloudSave(
    CloudSaveData data,
    Action<bool> onComplete)
    {
        if (data == null)
        {
            Debug.LogWarning(
                "[Cloud Save] Cannot save null data."
            );

            m_CloudSaveInProgress = false;

            onComplete?.Invoke(false);
            return;
        }

        if (GooglePlaySaveManager.Instance == null ||
            !GooglePlaySaveManager.Instance.IsReady)
        {
            Debug.LogWarning(
                "[Cloud Save] Google Play cloud save is not ready."
            );

            m_CloudSaveInProgress = false;

            onComplete?.Invoke(false);
            return;
        }

        m_CloudSaveInProgress = true;

        GooglePlaySaveManager.Instance.Save(
            data,
            success =>
            {
                Debug.Log(
                    $"[Cloud Save Result] Success={success}, " +
                    $"HighScore={data.highScore}, " +
                    $"HighDistance={data.highDistance}"
                );

                m_CloudSaveInProgress = false;

                onComplete?.Invoke(success);

                if (m_PendingCloudSave == null)
                    return;

                CloudSaveData pendingData =
                    m_PendingCloudSave;

                Action<bool> pendingCallback =
                    m_PendingCloudSaveCallback;

                m_PendingCloudSave = null;
                m_PendingCloudSaveCallback = null;

                Debug.Log(
                    $"[Cloud Save] Processing queued save. " +
                    $"HighScore={pendingData.highScore}, " +
                    $"HighDistance={pendingData.highDistance}"
                );

                StartCloudSave(
                    pendingData,
                    pendingCallback
                );
            }
        );
    }

    // ==================================================
    // LOAD
    // ==================================================

    public void LoadGame()
    {
        if (saveFile == null)
        {
            Debug.LogError(
                "SaveLoad: SaveFile is not initialized."
            );

            return;
        }

        m_LoadCompleted = false;
        m_CloudLoadStarted = false;

        if (GooglePlaySaveManager.Instance != null &&
            GooglePlaySaveManager.Instance.IsReady)
        {
            HandleCloudSaveLoad();
            return;
        }

        if (HasLocalSave())
        {
            LoadLocalGameTemporary();

            Debug.Log(
                "Loaded local save temporarily. " +
                "Waiting for Google Play cloud load."
            );
        }
        else
        {
            Debug.Log(
                "No local save available. " +
                "Waiting for Google Play cloud load."
            );
        }
    }

    private void LoadLocalGameTemporary()
    {
        LoadCurrency();
        LoadThemes();
        LoadTemporaryTheme();
        LoadHighScore();

        Debug.Log(
            "Game loaded from local save temporarily."
        );
    }

    private void HandleCloudSaveReady()
    {
        if (m_LoadCompleted)
            return;

        Debug.Log(
            "Google Play cloud save is ready."
        );

        HandleCloudSaveLoad();
    }

    private void HandleCloudSaveLoad()
    {
        if (m_LoadCompleted ||
            m_CloudLoadStarted)
        {
            return;
        }

        if (GooglePlaySaveManager.Instance == null ||
            !GooglePlaySaveManager.Instance.IsReady)
        {
            return;
        }

        m_CloudLoadStarted = true;

        Debug.Log(
            "Starting initial Google Play cloud load."
        );

        LoadCloudGame();
    }

    private void LoadCloudGame()
    {
        if (GooglePlaySaveManager.Instance == null)
        {
            Debug.LogError(
                "SaveLoad: GooglePlaySaveManager is missing."
            );

            m_CloudLoadStarted = false;
            return;
        }

        GooglePlaySaveManager.Instance.Load<CloudSaveData>(
            data =>
            {
                if (m_LoadCompleted)
                    return;

                if (data == null)
                {
                    Debug.Log(
                        "No cloud save found. " +
                        "Keeping current/default score values."
                    );

                    m_LoadCompleted = true;

                    SaveGame();

                    Debug.Log(
                        "Initial cloud load completed with no cloud data."
                    );

                    return;
                }

                Debug.Log(
                    $"[Cloud Load] HighScore={data.highScore}, " +
                    $"HighDistance={data.highDistance}"
                );

                ApplyCloudSaveData(data);

                m_LoadCompleted = true;

                Debug.Log(
                    $"[Cloud Load] Final local score - " +
                    $"HighScore={playerScoreData?.HighScore}, " +
                    $"HighDistance={playerScoreData?.HighDistance}"
                );

                Debug.Log(
                    "Game loaded from Google Play cloud."
                );
            }
        );
    }

    // ==================================================
    // LOCAL SAVE DETECTION
    // ==================================================

    private bool HasLocalSave()
    {
        return saveFile.HasData(CurrencyDataKey) ||
               saveFile.HasData(CurrentThemeDataKey) ||
               saveFile.HasData(BoughtThemesDataKey) ||
               saveFile.HasData(HighScoreDataKey) ||
               saveFile.HasData(HighDistanceDataKey) ||
               saveFile.HasData(TemporaryThemeIdDataKey) ||
               saveFile.HasData(TemporaryThemeExpiryDataKey) ||
               saveFile.HasData(TemporaryThemePreviousIdDataKey);
    }

    // ==================================================
    // CURRENCY
    // ==================================================

    private void SaveCurrency()
    {
        if (CurrencyWallet.Instance == null)
            return;

        saveFile.AddOrUpdateData(
            CurrencyDataKey,
            CurrencyWallet.Instance.Balance
        );
    }

    private void LoadCurrency()
    {
        if (CurrencyWallet.Instance == null ||
            !saveFile.HasData(CurrencyDataKey))
        {
            return;
        }

        int balance =
            saveFile.GetData<int>(
                CurrencyDataKey
            );

        CurrencyWallet.Instance.SetBalance(balance);
    }

    // ==================================================
    // THEMES
    // ==================================================

    private void SaveThemes()
    {
        if (ThemeManager.Instance == null)
            return;

        ThemeManager manager =
            ThemeManager.Instance;

        string currentThemeId =
            manager.GetCurrentThemeIdForSave();

        if (!string.IsNullOrEmpty(currentThemeId))
        {
            saveFile.AddOrUpdateData(
                CurrentThemeDataKey,
                currentThemeId
            );
        }

        string boughtThemes = string.Empty;

        foreach (ThemeDefinition theme in manager.OwnedThemes)
        {
            if (theme == null ||
                string.IsNullOrEmpty(theme.SaveId))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(boughtThemes))
                boughtThemes += ",";

            boughtThemes += theme.SaveId;
        }

        saveFile.AddOrUpdateData(
            BoughtThemesDataKey,
            boughtThemes
        );
    }

    private void LoadThemes()
    {
        if (ThemeManager.Instance == null)
            return;

        ThemeManager manager =
            ThemeManager.Instance;

        if (saveFile.HasData(BoughtThemesDataKey))
        {
            string boughtThemes =
                saveFile.GetData<string>(
                    BoughtThemesDataKey
                );

            if (!string.IsNullOrEmpty(boughtThemes))
            {
                string[] themeIds =
                    boughtThemes.Split(',');

                foreach (string themeId in themeIds)
                {
                    if (string.IsNullOrEmpty(themeId))
                        continue;

                    ThemeDefinition theme =
                        manager.GetThemeById(themeId);

                    if (theme != null)
                    {
                        manager.MarkThemeOwned(theme);
                    }
                }
            }
        }

        if (saveFile.HasData(CurrentThemeDataKey))
        {
            string currentThemeId =
                saveFile.GetData<string>(
                    CurrentThemeDataKey
                );

            ThemeDefinition theme =
                manager.GetThemeById(currentThemeId);

            if (theme != null &&
                manager.IsPermanentlyOwned(theme))
            {
                manager.SetTheme(theme);
            }
        }
    }

    // ==================================================
    // HIGH SCORE
    // ==================================================

    private void SaveHighScore()
    {
        if (playerScoreData == null)
            return;

        float highScore =
            playerScoreData.HighScore;

        float highDistance =
            playerScoreData.HighDistance;

        saveFile.AddOrUpdateData(
            HighScoreDataKey,
            highScore
        );

        saveFile.AddOrUpdateData(
            HighDistanceDataKey,
            highDistance
        );

        Debug.Log(
            $"[Score] High Score: {highScore}, " +
            $"High Distance: {highDistance}"
        );
    }

    private void LoadHighScore()
    {
        if (playerScoreData == null)
            return;

        bool hasHighScore =
            saveFile.HasData(HighScoreDataKey);

        bool hasHighDistance =
            saveFile.HasData(HighDistanceDataKey);

        if (!hasHighScore && !hasHighDistance)
            return;

        float highScore = 0f;
        float highDistance = 0f;

        if (hasHighScore)
        {
            highScore =
                saveFile.GetData<float>(
                    HighScoreDataKey
                );
        }

        if (hasHighDistance)
        {
            highDistance =
                saveFile.GetData<float>(
                    HighDistanceDataKey
                );
        }

        Debug.Log(
            $"[Score Local Load] HighScore={highScore}, " +
            $"HighDistance={highDistance}"
        );

        playerScoreData.RestoreBest(
            highScore,
            highDistance
        );

        Debug.Log(
            $"[Score Local Load] Result - " +
            $"HighScore={playerScoreData.HighScore}, " +
            $"HighDistance={playerScoreData.HighDistance}"
        );
    }

    // ==================================================
    // TEMPORARY THEME
    // ==================================================

    private void SaveTemporaryTheme()
    {
        if (ThemeManager.Instance == null)
            return;

        ThemeManager manager =
            ThemeManager.Instance;

        string temporaryThemeId =
            manager.GetTemporaryThemeIdForSave();

        long expiryTicks =
            manager.GetTemporaryThemeExpiryTicksForSave();

        string previousThemeId =
            manager.GetPreviousThemeIdForSave();

        saveFile.AddOrUpdateData(
            TemporaryThemeIdDataKey,
            temporaryThemeId
        );

        saveFile.AddOrUpdateData(
            TemporaryThemeExpiryDataKey,
            expiryTicks
        );

        saveFile.AddOrUpdateData(
            TemporaryThemePreviousIdDataKey,
            previousThemeId
        );
    }

    private void LoadTemporaryTheme()
    {
        if (ThemeManager.Instance == null)
            return;

        if (!saveFile.HasData(TemporaryThemeIdDataKey) ||
            !saveFile.HasData(TemporaryThemeExpiryDataKey))
        {
            return;
        }

        string temporaryThemeId =
            saveFile.GetData<string>(
                TemporaryThemeIdDataKey
            );

        long expiryTicks =
            saveFile.GetData<long>(
                TemporaryThemeExpiryDataKey
            );

        string previousThemeId = string.Empty;

        if (saveFile.HasData(
                TemporaryThemePreviousIdDataKey))
        {
            previousThemeId =
                saveFile.GetData<string>(
                    TemporaryThemePreviousIdDataKey
                );
        }

        ThemeManager.Instance.RestoreTemporaryUnlock(
            temporaryThemeId,
            expiryTicks,
            previousThemeId
        );
    }

    // ==================================================
    // CLOUD DATA
    // ==================================================

    private CloudSaveData CreateCloudSaveData()
    {
        CloudSaveData data =
            new CloudSaveData();

        if (CurrencyWallet.Instance != null)
        {
            data.currency =
                CurrencyWallet.Instance.Balance;
        }

        if (ThemeManager.Instance != null)
        {
            ThemeManager manager =
                ThemeManager.Instance;

            string boughtThemes = string.Empty;

            foreach (ThemeDefinition theme in manager.OwnedThemes)
            {
                if (theme == null ||
                    string.IsNullOrEmpty(theme.SaveId))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(boughtThemes))
                    boughtThemes += ",";

                boughtThemes += theme.SaveId;
            }

            data.boughtThemes =
                boughtThemes;
        }

        if (playerScoreData != null)
        {
            data.highScore =
                playerScoreData.HighScore;

            data.highDistance =
                playerScoreData.HighDistance;
        }

        return data;
    }

    private void ApplyCloudSaveData(CloudSaveData data)
    {
        if (data == null)
            return;

        if (CurrencyWallet.Instance != null)
        {
            CurrencyWallet.Instance.SetBalance(
                data.currency
            );
        }

        if (ThemeManager.Instance != null)
        {
            ThemeManager manager =
                ThemeManager.Instance;

            if (!string.IsNullOrEmpty(data.boughtThemes))
            {
                string[] themeIds =
                    data.boughtThemes.Split(',');

                foreach (string themeId in themeIds)
                {
                    if (string.IsNullOrEmpty(themeId))
                        continue;

                    ThemeDefinition theme =
                        manager.GetThemeById(themeId);

                    if (theme != null)
                    {
                        manager.MarkThemeOwned(theme);
                    }
                }
            }
        }

        if (playerScoreData != null)
        {
            Debug.Log(
                $"[Score Cloud] Before restore - " +
                $"Current={playerScoreData.Current.Score}, " +
                $"RunPeak={playerScoreData.RunPeak.Score}, " +
                $"Best={playerScoreData.Best.Score}, " +
                $"CloudScore={data.highScore}, " +
                $"CloudDistance={data.highDistance}"
            );

            playerScoreData.RestoreBest(
                data.highScore,
                data.highDistance
            );

            Debug.Log(
                $"[Score Cloud] After restore - " +
                $"Best={playerScoreData.Best.Score}, " +
                $"BestDistance={playerScoreData.Best.Distance}"
            );
        }

        SaveGame();
    }
}
