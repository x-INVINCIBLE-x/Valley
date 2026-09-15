using System;
using UnityEngine;
using Valley.Aiming;

namespace Valley.Core
{
    public class PlayerLaunchGate : MonoBehaviour, IAimBlocker
    {
        public static event Action<int, int> OnChargesChanged;
        
        // <Previous Charges, New Max Charges>
        public static event Action<int, int> OnMaxChargesChanged;

        [SerializeField] private int maxCharges = 1;
        [SerializeField] private LayerMask groundMask;

        public int Remaining { get; private set; }
        public bool CanLaunch => Remaining > 0;
        bool IAimBlocker.CanAim => CanLaunch;
        private int initialMaxCharges;

        private void Awake()
        {
            initialMaxCharges = maxCharges;
            Remaining = maxCharges;
            OnChargesChanged?.Invoke(Remaining, maxCharges);
        }

        public bool TryConsume()
        {
            if (!CanLaunch) return false;

            Remaining--;
            OnChargesChanged?.Invoke(Remaining, maxCharges);
            return true;
        }

        public void SetCharges(int amount)
        {
            Remaining = Mathf.Clamp(amount, 0, maxCharges);
            OnChargesChanged?.Invoke(Remaining, maxCharges);
        }

        public void IncreaseMaxCharges(int amount)
        {
            if (amount <= 0) return;

            int prevCharges = maxCharges;
            maxCharges += amount;
            Remaining += amount;

            OnChargesChanged?.Invoke(Remaining, maxCharges);
            OnMaxChargesChanged?.Invoke(prevCharges, maxCharges);
        }

        public void DecreaseMaxCharges(int amount)
        {
            if (amount <= 0) return;

            int prevCharges = maxCharges;
            maxCharges = Mathf.Max(1, maxCharges - amount);
            Remaining = Mathf.Min(Remaining, maxCharges);

            OnChargesChanged?.Invoke(Remaining, maxCharges);
            OnMaxChargesChanged?.Invoke(prevCharges, maxCharges);
        }

        public void ResetMaxCharges()
        {
            int prevCharges = maxCharges;
            maxCharges = initialMaxCharges;
            Remaining = Mathf.Min(Remaining, maxCharges);

            OnChargesChanged?.Invoke(Remaining, maxCharges);
            OnMaxChargesChanged?.Invoke(prevCharges, maxCharges);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsInLayerMask(collision.gameObject.layer, groundMask)) return;
            if (Remaining == maxCharges) return;

            Remaining = maxCharges;
            OnChargesChanged?.Invoke(Remaining, maxCharges);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, groundMask)) return;
            if (Remaining == maxCharges) return;

            Remaining = maxCharges;
            OnChargesChanged?.Invoke(Remaining, maxCharges);
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    }
}