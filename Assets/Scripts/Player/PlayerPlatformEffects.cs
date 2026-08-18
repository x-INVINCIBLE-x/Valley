using MoreMountains.Feedbacks;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Valley.Platforms;

namespace Valley.Player
{
    public class PlayerPlatformEffects : MonoBehaviour
    {
        public static event Action<PlatformEffectProfile> OnPlatformEffectApplied;
        public static event Action OnPlatformEffectCleared;

        [SerializeField] private LayerMask platformMask;
        [SerializeField] private PlatfromEffectFeedback[] feedbacks;

        public PlatformEffectProfile Current { get; private set; }

        private void OnCollisionEnter(Collision collision) => TryApply(collision.collider);
        //private void OnCollisionExit(Collision collision) => TryClear(collision.collider);

        private void TryApply(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, platformMask)) return;

            var zone = other.GetComponentInChildren<PlatformEffectZone>();
            if (zone == null || zone.Profile == null) return;

            Current = zone.Profile;

            ExecuteFeedback();

            OnPlatformEffectApplied?.Invoke(Current);
        }

        private void TryClear(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, platformMask)) return;

            var zone = other.GetComponentInChildren<PlatformEffectZone>();
            if (zone == null || zone.Profile != Current) return;

            Current = null;
            OnPlatformEffectCleared?.Invoke();
        }

        private void ExecuteFeedback()
        {
            if (feedbacks == null || feedbacks.Length == 0) return;
            if (Current == null) return;

            for (int i = 0; i < feedbacks.Length; i++)
            {
                PlatfromEffectFeedback fedback = feedbacks[i];
                if (fedback.EffectType == Current.EffectType)
                {
                    if (fedback.Feedback != null)
                    {
                        fedback.Feedback.PlayFeedbacks();
                    }
                    break;
                }
            }
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    }

    [Serializable]
    public class PlatfromEffectFeedback
    {
        [field: SerializeField] public EffectType EffectType { get; private set; }
        [field: SerializeField] public MMF_Player Feedback { get; private set; }
    }
}
