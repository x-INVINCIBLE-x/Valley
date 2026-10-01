using MoreMountains.Feedbacks;
using System;
using TMPro;
using UnityEngine;
using Valley.Economy;

public class WalletUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI finalAmtText;
    [SerializeField] private TextMeshProUGUI deltaAmtText;
    [SerializeField] private MMF_Player updateFeedback;

    private CurrencyWallet wallet;

    private void Start()
    {
        wallet = CurrencyWallet.Instance;
        CurrencyWallet.OnBalanceAdded += UpdateUI;
        CurrencyWallet.OnBalanceChanged += UpdateCurrencyChange;

        UpdateUI(0, wallet.Balance);
    }

    private void OnDestroy()
    {
        CurrencyWallet.OnBalanceAdded -= UpdateUI;
        CurrencyWallet.OnBalanceChanged -= UpdateCurrencyChange;
    }

    private void UpdateUI(int amt, int newBalance)
    {
        finalAmtText.text = newBalance.ToString();

        if (deltaAmtText != null)
        {
            deltaAmtText.text = $"+{amt}";
        }

        if (updateFeedback != null)
        {
            updateFeedback.PlayFeedbacks();
        }
    }

    private void UpdateCurrencyChange(int newBalance)
    {
        finalAmtText.text = newBalance.ToString();

        if (deltaAmtText != null)
        {
            deltaAmtText.text = "";
        }

        if (updateFeedback != null)
        {
            updateFeedback.PlayFeedbacks();
        }
    }
}