using MoreMountains.Feedbacks;
using UnityEngine;

namespace Valley.Platforms
{
    [RequireComponent(typeof(Collider))]
    public class PlatformEffectZone : MonoBehaviour
    {
        [SerializeField] private PlatformEffectProfile profile;
        [SerializeField] private MMF_Player collisionFeedback;

        public PlatformEffectProfile Profile => profile;

        private void OnCollisionEnter(Collision collision)
        {
            if (collisionFeedback != null) {
                collisionFeedback.PlayFeedbacks();
            }
        }
    }
}
