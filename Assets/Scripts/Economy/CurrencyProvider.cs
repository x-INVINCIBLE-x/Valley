using UnityEngine;
using Valley.Economy;

public class CurrencyProvider : MonoBehaviour
{
    [SerializeField] private bool onTrigger = false;
    [SerializeField] private LayerMask collectorLayer;
    [SerializeField] private int amt = 10;

    private void OnTriggerEnter(Collider other)
    {
        AddMoney(amt);
        gameObject.SetActive(false);
    }

    public void AddMoney(int amt)
    {
        CurrencyWallet.Instance.Add(amt);
    }
}