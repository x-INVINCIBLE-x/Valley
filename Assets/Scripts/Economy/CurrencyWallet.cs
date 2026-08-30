using System;
using UnityEngine;

namespace Valley.Economy
{
    public class CurrencyWallet : MonoBehaviour
    {
        public static CurrencyWallet Instance { get; private set; }

        public static event Action<int> OnBalanceChanged;

        [SerializeField] private int startingBalance;

        public int Balance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            Balance = startingBalance;
            OnBalanceChanged?.Invoke(Balance);
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            Balance += amount;
            OnBalanceChanged?.Invoke(Balance);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Balance < amount)
                return false;

            Balance -= amount;
            OnBalanceChanged?.Invoke(Balance);

            if (GameManager.Instance != null)
                GameManager.Instance.SaveGame();

            return true;
        }

        public void SetBalance(int amount)
        {
            Balance = Mathf.Max(0, amount);
            OnBalanceChanged?.Invoke(Balance);
        }
    }
}