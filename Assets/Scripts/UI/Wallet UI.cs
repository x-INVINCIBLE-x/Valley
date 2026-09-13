using MoreMountains.Feedbacks;
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

        UpdateUI(0, wallet.Balance);
    }

    private void OnDestroy()
    {
        CurrencyWallet.OnBalanceAdded -= UpdateUI;
    }

    private void UpdateUI(int amt, int newBalance)
    {
        finalAmtText.text = newBalance.ToString();
        deltaAmtText.text = $"+{amt}";

        if (updateFeedback != null)
        {
            updateFeedback.PlayFeedbacks();
        }
    }
}
