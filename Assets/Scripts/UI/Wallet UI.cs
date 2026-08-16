using MoreMountains.Feedbacks;
using System;
using TMPro;
using UnityEngine;
using Valley.Economy;

public class WalletUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI amtText;
    [SerializeField] private MMF_Player updateFeedback;

    private CurrencyWallet wallet;

    private void Start()
    {
        wallet = CurrencyWallet.Instance;
        CurrencyWallet.OnBalanceChanged += UpdateUI;

        UpdateUI(wallet.Balance);
    }

    private void OnDestroy()
    {
        CurrencyWallet.OnBalanceChanged -= UpdateUI;
    }

    private void UpdateUI(int amt)
    {
        amtText.text = amt.ToString();

        if (updateFeedback != null)
        {
            updateFeedback.PlayFeedbacks();
        }
    }
}
