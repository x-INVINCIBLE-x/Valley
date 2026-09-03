using MoreMountains.Feedbacks;
using UnityEngine;

namespace Valley.Platforms
{
    [RequireComponent(typeof(Collider))]
    public class PlatformEffectZone : MonoBehaviour
    {
        [SerializeField] private PlatformEffectProfile profile;
        [SerializeField] private MMF_Player collisionFeedback;
        [SerializeField] private MMF_Player pointCollisionFeedback;

        public PlatformEffectProfile Profile => profile;

        private void OnCollisionEnter(Collision collision)
        {
            if (collisionFeedback != null) {
                collisionFeedback.PlayFeedbacks();
            }

            if (pointCollisionFeedback != null)
            {
                Vector3 normal = collision.contacts[0].normal;
                Quaternion lookRotation = Quaternion.LookRotation(-normal);
                
                pointCollisionFeedback.transform.rotation = lookRotation;
                pointCollisionFeedback.PlayFeedbacks(collision.contacts[0].point);
            }
        }
    }
}
