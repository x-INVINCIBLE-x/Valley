using System;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using UnityEngine;
using Valley;

public class PlayerDataManager : MonoBehaviour
{
    [SerializeField] private string playerName;
    
    private MyModuleBindings moduleBindings;

    private void Start()
    {
        LoginManager.PlayerSignedIn += InitializePlayer;

        moduleBindings = new MyModuleBindings(CloudCodeService.Instance);
    }

    private async void InitializePlayer()
    {
        try
        {
            //var resultFromCloud = await moduleBindings.SetPlayerName(name);
            //Debug.Log(resultFromCloud);
        }
        catch(CloudCodeException e) 
        {
            Debug.LogException(e);
        }
    }

    private async void SetPlayerName()
    {
        try
        {
            var result = await moduleBindings.SetPlayerName(UserOnboardingData.PlayerName);
            Debug.Log($"Set Player Name: {result}");
        }
        catch (CloudCodeException e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        LoginManager.PlayerSignedIn -= InitializePlayer;
    }
}
