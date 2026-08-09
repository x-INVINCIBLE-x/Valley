using System;
using UnityEngine;
using Valley.Aiming;

namespace Valley.Core
{
    public class PlayerLaunchGate : MonoBehaviour, IAimBlocker
    {
        public static event Action<int, int> OnChargesChanged;

        [SerializeField] private int maxCharges = 1;
        [SerializeField] private LayerMask groundMask;

        public int Remaining { get; private set; }
        public bool CanLaunch => Remaining > 0;
        bool IAimBlocker.CanAim => CanLaunch;

        private void Awake()
        {
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

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsInLayerMask(collision.gameObject.layer, groundMask)) return;
            if (Remaining == maxCharges) return;

            Remaining = maxCharges;
            OnChargesChanged?.Invoke(Remaining, maxCharges);
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    }
}