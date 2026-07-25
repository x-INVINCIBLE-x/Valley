using System;
using UnityEngine;
using Valley.Platforms;

namespace Valley.Player
{
    public class PlayerPlatformEffects : MonoBehaviour
    {
        public static event Action<PlatformEffectProfile> OnPlatformEffectApplied;
        public static event Action OnPlatformEffectCleared;

        [SerializeField] private LayerMask platformMask;

        public PlatformEffectProfile Current { get; private set; }

        private void OnCollisionEnter(Collision collision) => TryApply(collision.collider);
        private void OnCollisionExit(Collision collision) => TryClear(collision.collider);

        private void TryApply(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, platformMask)) return;

            var zone = other.GetComponent<PlatformEffectZone>();
            if (zone == null || zone.Profile == null) return;

            Current = zone.Profile;
            OnPlatformEffectApplied?.Invoke(Current);
        }

        private void TryClear(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, platformMask)) return;

            var zone = other.GetComponent<PlatformEffectZone>();
            if (zone == null || zone.Profile != Current) return;

            Current = null;
            OnPlatformEffectCleared?.Invoke();
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    }
}
