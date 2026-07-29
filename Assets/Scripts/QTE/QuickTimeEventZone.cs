using UnityEngine;

namespace Valley.QTE
{
    [RequireComponent(typeof(Collider))]
    public class QuickTimeEventZone : MonoBehaviour
    {
        [Tooltip("Which QTE this trap starts.")]
        [SerializeField] private QuickTimeEventProfile profile;
        [SerializeField] private LayerMask targetMask;
        [Tooltip("If true, this zone stops starting new QTEs after the first one it successfully starts.")]
        [SerializeField] private bool disableAfterTrigger = false;

        [SerializeField] private bool _hasTriggered;

        private void OnTriggerEnter(Collider other) => TryTrigger(other.gameObject);
        private void OnCollisionEnter(Collision collision) => TryTrigger(collision.gameObject);

        private void TryTrigger(GameObject target)
        {
            if (disableAfterTrigger && _hasTriggered) return;
            if (!IsInLayerMask(target.layer, targetMask)) return;
            if (QuickTimeEventRunner.Instance == null) return;

            if (QuickTimeEventRunner.Instance.Begin(profile)) _hasTriggered = true;
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    }
}