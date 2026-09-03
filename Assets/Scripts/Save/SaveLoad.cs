using Esper.ESave;
using System;
using UnityEngine;
using Valley;
using Valley.Economy;
using Valley.Theming;

[RequireComponent(typeof(SaveFileSetup))]
public class SaveLoad : MonoBehaviour
{
    private const string CurrencyDataKey = "Currency";
    private const string CurrentThemeDataKey = "CurrentTheme";
    private const string BoughtThemesDataKey = "BoughtThemes";

    // Tracks whether this Google Play account has already completed
    // its initial cloud synchronization on this device.
    private const string CloudInitializedKeyPrefix =
        "CloudSaveInitialized_";

    private SaveFile saveFile;

    private bool m_WaitingForCloudLoad;
    private bool m_LoadCompleted;

    [Serializable]
    private class CloudSaveData
    {
        public int currency;
        public string currentTheme;
        public string boughtThemes;
    }

    private void Awake()
    {
        SaveFileSetup setup = GetComponent<SaveFileSetup>();
        saveFile = setup.GetSaveFile();
    }

    private void OnEnable()
    {
        GooglePlaySaveManager.CloudSaveReady += HandleCloudSaveReady;

        // Handles the case where authentication already completed
        // before this object became enabled.
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

        saveFile.Save();

        Debug.Log("Game saved locally.");
    }

    public void SaveGameToCloud()
    {
        SaveGame();

        if (GooglePlaySaveManager.Instance == null)
        {
            Debug.LogWarning(
                "SaveLoad: GooglePlaySaveManager is missing."
            );
            return;
        }

        CloudSaveData data = CreateCloudSaveData();

        GooglePlaySaveManager.Instance.Save(
            data,
            success =>
            {
                if (success)
                    Debug.Log("Game saved locally and to Google Play.");
            });
    }

    // ==================================================
    // LOAD
    // ==================================================

    public void LoadGame()
    {
        if (saveFile == null)
        {
            Debug.LogError("SaveLoad: SaveFile is not initialized.");
            return;
        }

        // Google Play is already authenticated.
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
                "Waiting for Google Play authentication."
            );
        }
        else
        {
            Debug.Log(
                "No local save available. " +
                "Waiting for Google Play authentication."
            );
        }

        m_WaitingForCloudLoad = true;
    }
    private void LoadLocalGameTemporary()
    {
        LoadCurrency();
        LoadThemes();

        Debug.Log("Game loaded from local save temporarily.");
    }

    private void HandleCloudSaveReady()
    {
        if (m_LoadCompleted)
            return;

        Debug.Log("Google Play cloud save is ready.");

        HandleCloudSaveLoad();
    }

    private void HandleCloudSaveLoad()
    {
        if (m_LoadCompleted)
            return;

        if (GooglePlaySaveManager.Instance == null ||
            !GooglePlaySaveManager.Instance.IsReady)
        {
            m_WaitingForCloudLoad = true;
            return;
        }

        m_WaitingForCloudLoad = false;

        // --------------------------------------------------
        // FIRST CLOUD LOAD FOR THIS GOOGLE ACCOUNT
        // --------------------------------------------------

        if (IsFirstCloudLoadForAccount())
        {
            Debug.Log(
                "First Google Play sign-in detected. " +
                "Cloud data will override local data."
            );

            LoadCloudGame();
            return;
        }

        // --------------------------------------------------
        // SUBSEQUENT STARTUPS
        // --------------------------------------------------

        if (HasLocalSave())
        {
            LoadLocalGameFinal();
            return;
        }

        // No local cache available.
        LoadCloudGame();
    }

    private void LoadLocalGameFinal()
    {
        if (m_LoadCompleted)
            return;

        LoadCurrency();
        LoadThemes();

        m_LoadCompleted = true;

        Debug.Log("Game loaded from local save.");
    }

    private void LoadCloudGame()
    {
        if (GooglePlaySaveManager.Instance == null)
        {
            Debug.LogError(
                "SaveLoad: GooglePlaySaveManager is missing."
            );
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
                        "Starting with default data."
                    );

                    MarkCloudLoadInitialized();

                    m_LoadCompleted = true;
                    return;
                }

                Debug.Log(
                    "Cloud save found. " +
                    "Applying cloud data over local data."
                );

                ApplyCloudSaveData(data);

                MarkCloudLoadInitialized();

                m_LoadCompleted = true;

                Debug.Log(
                    "Game loaded from Google Play cloud."
                );
            });
    }

    // ==================================================
    // FIRST CLOUD LOAD
    // ==================================================

    private bool IsFirstCloudLoadForAccount()
    {
#if UNITY_ANDROID

        if (LoginManager.Instance == null)
            return true;

        string googleUserId =
            LoginManager.Instance.GooglePlayGamesUserId;

        if (string.IsNullOrEmpty(googleUserId))
            return true;

        string key =
            CloudInitializedKeyPrefix + googleUserId;

        return !PlayerPrefs.HasKey(key);

#else

        return true;

#endif
    }

    private void MarkCloudLoadInitialized()
    {
#if UNITY_ANDROID

        if (LoginManager.Instance == null)
            return;

        string googleUserId =
            LoginManager.Instance.GooglePlayGamesUserId;

        if (string.IsNullOrEmpty(googleUserId))
            return;

        string key =
            CloudInitializedKeyPrefix + googleUserId;

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();

#endif
    }

    // ==================================================
    // LOCAL SAVE DETECTION
    // ==================================================

    private bool HasLocalSave()
    {
        return saveFile.HasData(CurrencyDataKey) ||
               saveFile.HasData(CurrentThemeDataKey) ||
               saveFile.HasData(BoughtThemesDataKey);
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
            return;

        int balance =
            saveFile.GetData<int>(CurrencyDataKey);

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

        if (manager.CurrentTheme != null)
        {
            saveFile.AddOrUpdateData(
                CurrentThemeDataKey,
                manager.CurrentTheme.SaveId
            );
        }

        string boughtThemes = string.Empty;

        foreach (ThemeDefinition theme in manager.OwnedThemes)
        {
            if (theme == null ||
                string.IsNullOrEmpty(theme.SaveId))
                continue;

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
                        manager.MarkThemeOwned(theme);
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

            if (theme != null)
                manager.SetTheme(theme);
        }
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

            if (manager.CurrentTheme != null)
            {
                data.currentTheme =
                    manager.CurrentTheme.SaveId;
            }

            string boughtThemes = string.Empty;

            foreach (ThemeDefinition theme in manager.OwnedThemes)
            {
                if (theme == null ||
                    string.IsNullOrEmpty(theme.SaveId))
                    continue;

                if (!string.IsNullOrEmpty(boughtThemes))
                    boughtThemes += ",";

                boughtThemes += theme.SaveId;
            }

            data.boughtThemes =
                boughtThemes;
        }

        return data;
    }

    private void ApplyCloudSaveData(
        CloudSaveData data)
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

            if (!string.IsNullOrEmpty(
                    data.boughtThemes))
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
                        manager.MarkThemeOwned(theme);
                }
            }

            if (!string.IsNullOrEmpty(
                    data.currentTheme))
            {
                ThemeDefinition theme =
                    manager.GetThemeById(
                        data.currentTheme
                    );

                if (theme != null)
                    manager.SetTheme(theme);
            }
        }

        // Cloud becomes the local cache.
        SaveGame();
    }
}