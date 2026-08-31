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

    private SaveFile saveFile;

    private bool m_WaitingForCloudLoad;

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
        Valley.LoginManager.PlayerSignedIn += HandlePlayerSignedIn;
    }

    private void OnDisable()
    {
        Valley.LoginManager.PlayerSignedIn -= HandlePlayerSignedIn;
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

    /// <summary>
    /// Saves locally and then uploads the same data to Google Play.
    /// Used only after a theme purchase and when the game closes.
    /// </summary>
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

        // Local save always has priority.
        if (HasLocalSave())
        {
            LoadLocalGame();
            return;
        }

        // No local save.
        // Try cloud if Google Play is already ready.
        m_WaitingForCloudLoad = true;

        TryLoadCloudGame();
    }

    private void LoadLocalGame()
    {
        LoadCurrency();
        LoadThemes();

        Debug.Log("Game loaded from local save.");
    }

    private void TryLoadCloudGame()
    {
        if (!m_WaitingForCloudLoad)
            return;

        if (GooglePlaySaveManager.Instance == null)
        {
            Debug.LogWarning(
                "SaveLoad: GooglePlaySaveManager is missing. " +
                "Starting with default data."
            );

            m_WaitingForCloudLoad = false;
            return;
        }

        if (!GooglePlaySaveManager.Instance.IsReady)
        {
            // Login may not have completed yet.
            return;
        }

        m_WaitingForCloudLoad = false;

        GooglePlaySaveManager.Instance.Load<CloudSaveData>(
            data =>
            {
                if (data == null)
                {
                    Debug.Log("No cloud save found. Using new game data.");
                    return;
                }

                ApplyCloudSaveData(data);

                Debug.Log("Game loaded from Google Play.");
            });
    }

    private void HandlePlayerSignedIn()
    {
        TryLoadCloudGame();
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

        int balance = saveFile.GetData<int>(CurrencyDataKey);
        CurrencyWallet.Instance.SetBalance(balance);
    }

    // ==================================================
    // THEMES
    // ==================================================

    private void SaveThemes()
    {
        if (ThemeManager.Instance == null)
            return;

        ThemeManager manager = ThemeManager.Instance;

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
            if (theme == null || string.IsNullOrEmpty(theme.SaveId))
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

        ThemeManager manager = ThemeManager.Instance;

        if (saveFile.HasData(BoughtThemesDataKey))
        {
            string boughtThemes =
                saveFile.GetData<string>(BoughtThemesDataKey);

            if (!string.IsNullOrEmpty(boughtThemes))
            {
                string[] themeIds = boughtThemes.Split(',');

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
                saveFile.GetData<string>(CurrentThemeDataKey);

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
        CloudSaveData data = new CloudSaveData();

        if (CurrencyWallet.Instance != null)
        {
            data.currency = CurrencyWallet.Instance.Balance;
        }

        if (ThemeManager.Instance != null)
        {
            ThemeManager manager = ThemeManager.Instance;

            if (manager.CurrentTheme != null)
            {
                data.currentTheme = manager.CurrentTheme.SaveId;
            }

            string boughtThemes = string.Empty;

            foreach (ThemeDefinition theme in manager.OwnedThemes)
            {
                if (theme == null || string.IsNullOrEmpty(theme.SaveId))
                    continue;

                if (!string.IsNullOrEmpty(boughtThemes))
                    boughtThemes += ",";

                boughtThemes += theme.SaveId;
            }

            data.boughtThemes = boughtThemes;
        }

        return data;
    }

    private void ApplyCloudSaveData(CloudSaveData data)
    {
        if (data == null)
            return;

        if (CurrencyWallet.Instance != null)
        {
            CurrencyWallet.Instance.SetBalance(data.currency);
        }

        if (ThemeManager.Instance != null)
        {
            ThemeManager manager = ThemeManager.Instance;

            if (!string.IsNullOrEmpty(data.boughtThemes))
            {
                string[] themeIds = data.boughtThemes.Split(',');

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

            if (!string.IsNullOrEmpty(data.currentTheme))
            {
                ThemeDefinition theme =
                    manager.GetThemeById(data.currentTheme);

                if (theme != null)
                    manager.SetTheme(theme);
            }
        }

        // Store cloud data locally so next startup uses local save.
        SaveGame();
    }
}