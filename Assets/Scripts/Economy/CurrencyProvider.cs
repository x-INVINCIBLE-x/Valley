using MoreMountains.Feedbacks;
using UnityEngine;
using Valley.Economy;

public class CurrencyProvider : MonoBehaviour
{
    [Header("Collection")]
    [SerializeField] private LayerMask collectorLayer;
    [SerializeField] private int amt = 10;

    [Header("Settings")]
    [SerializeField] private bool onTrigger = false;
    [SerializeField] private bool disableOnTrigger = false;
    [SerializeField] private bool restoreInitialScale = true;

    [Header("Feedback")]
    [SerializeField] private MMF_Player collectFeedback;

    private Vector3 initialScale;

    private void Awake()
    {
        if (restoreInitialScale)
            initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = initialScale;
    }

    private void OnTriggerEnter(Collider other)
    {
        AddMoney(amt);

        if (disableOnTrigger)
            gameObject.SetActive(false);

        if (collectFeedback != null)
            collectFeedback.PlayFeedbacks();
    }

    public void AddMoney(int amt)
    {
        CurrencyWallet.Instance.Add(amt);
    }
}