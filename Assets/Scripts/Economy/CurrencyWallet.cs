using System;
using UnityEngine;

namespace Valley.Economy
{
    public class CurrencyWallet : MonoBehaviour
    {
        public static event Action<int> OnBalanceChanged;

        [SerializeField] private int startingBalance;

        public int Balance { get; private set; }

        private void Awake()
        {
            Balance = startingBalance;
            OnBalanceChanged?.Invoke(Balance);
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;

            Balance += amount;
            OnBalanceChanged?.Invoke(Balance);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Balance < amount) return false;

            Balance -= amount;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }
    }
}