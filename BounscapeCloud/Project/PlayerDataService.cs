using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace BounscapeCloud;

public class PlayerDataService
{
    readonly IGameApiClient m_GameApiClient;
    readonly ILogger<PlayerDataService> m_Logger;

    private const string k_playerName = "PLAYER_NAME";

    public PlayerDataService(IGameApiClient gameApiClient, ILogger<PlayerDataService> logger)
    {
        m_GameApiClient = gameApiClient;
        m_Logger = logger;
    }

    private async Task SavePlayerData(IExecutionContext context, IGameApiClient gameApiClient, string key, string value)
    {
        try
        {
            await gameApiClient.CloudSaveData.SetItemAsync(
                context,
                context.AccessToken!,
                context.ProjectId!,
                context.PlayerId!,
                new SetItemBody(key, value));

            m_Logger.LogInformation("Successfully saved data for key: {Key}", key);
        }
        catch (ApiException ex)
        {
            m_Logger.LogError("Failed to save data for key {Key}. Error: {Error}", key, ex.Message);
            throw new Exception($"Unable to save player data: {ex.Message}");
        }
    }

    private async Task<string> GetPlayerData(IExecutionContext context, IGameApiClient gameApiClient, string key)
    {
        try
        {
            var result = await gameApiClient.CloudSaveData.GetItemsAsync(
                context,
                context.AccessToken!,
                context.ProjectId!,
                context.PlayerId!,
                new List<string> { key });

            var data = result.Data.Results.FirstOrDefault()?.Value?.ToString() ?? string.Empty;
            m_Logger.LogInformation("Successfully retrieved data for key: {Key}", key);
            return data;
        }
        catch (ApiException ex)
        {
            m_Logger.LogError("Failed to retrieve data for key {Key}. Error: {Error}", key, ex.Message);
            throw new Exception($"Unable to retrieve player data: {ex.Message}");
        }
    }

    [CloudCodeFunction("SayHello")]
    public string Hello(string name)
    {
        return $"Hello, {name}!";
    }

    [CloudCodeFunction("GetServerTime")]
    public string GetServerTime(IExecutionContext context)
    {
        return DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
    }

    [CloudCodeFunction("SetPlayerName")]
    public async Task<string> SetPlayerName(IExecutionContext context, IGameApiClient gameApiClient,string name)
    {
        if (IsValidName(name))
        {
            await SavePlayerData(context, gameApiClient, k_playerName, name);
            return name;
        }

        throw new ArgumentException("Name is not valid");
    }

    public static bool IsValidName(string name)
    {
        if (name.Length > 15)
        {
            return false;
        }

        if (!name.All(char.IsLetterOrDigit))
        {
            return false;
        }

        return true;
    }
}