using UnityEngine;
using Esper.ESave;
using Valley.Economy;
using Valley.Theming;

[RequireComponent(typeof(SaveFileSetup))]
public class SaveLoad : MonoBehaviour
{
    private const string CurrencyDataKey = "Currency";
    private const string CurrentThemeDataKey = "CurrentTheme";
    private const string BoughtThemesDataKey = "BoughtThemes";

    private SaveFile saveFile;

    private void Awake()
    {
        SaveFileSetup setup = GetComponent<SaveFileSetup>();
        saveFile = setup.GetSaveFile();
    }

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
        Debug.Log("Game saved.");
    }

    public void LoadGame()
    {
        if (saveFile == null)
        {
            Debug.LogError("SaveLoad: SaveFile is not initialized.");
            return;
        }

        LoadCurrency();
        LoadThemes();

        Debug.Log("Game loaded.");
    }

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
}