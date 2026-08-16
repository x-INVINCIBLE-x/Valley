using UnityEngine;
using Valley.Economy;

public class CurrencyProvider : MonoBehaviour
{
    public void AddMoney(int amt)
    {
        CurrencyWallet.Instance.Add(amt);
    }
}