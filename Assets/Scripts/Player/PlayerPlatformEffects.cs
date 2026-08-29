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

        private void OnCollisionEnter(Collision collision) => TryApply(collision);
        //private void OnCollisionExit(Collision collision) => TryClear(collision);

        private void TryApply(Collision collision)
        {
            var other = collision.collider;

            if (!IsInLayerMask(other.gameObject.layer, platformMask)) return;

            var zone = other.GetComponent<PlatformEffectZone>();
            if (zone == null || zone.Profile == null) return;

            Current = zone.Profile;

            ExecuteFeedback(collision.contacts[0].point,
                            collision.contacts[0].normal);

            OnPlatformEffectApplied?.Invoke(Current);
        }

        private void TryClear(Collision collision)
        {
            var other = collision.collider;

            if (!IsInLayerMask(other.gameObject.layer, platformMask)) return;

            var zone = other.GetComponent<PlatformEffectZone>();
            if (zone == null || zone.Profile != Current) return;

            Current = null;
            OnPlatformEffectCleared?.Invoke();
        }

        private void ExecuteFeedback(Vector3 position, Vector3 normal)
        {
            if (feedbacks == null || feedbacks.Length == 0) return;
            if (Current == null) return;

            Quaternion rotation = Quaternion.LookRotation(normal);

            for (int i = 0; i < feedbacks.Length; i++)
            {
                PlatfromEffectFeedback feedback = feedbacks[i];
                if (feedback.EffectType == Current.EffectType)
                {
                    if (feedback.Feedback != null)
                    {
                        feedback.Feedback.PlayFeedbacks();
                    }

                    if (feedback.OnPointFeedback != null)
                    {
                        feedback.OnPointFeedback.transform.rotation = rotation;
                        feedback.OnPointFeedback.PlayFeedbacks(position);
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
        [field: SerializeField] public MMF_Player OnPointFeedback { get; private set; }
    }
}
