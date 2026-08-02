using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Valley.Paths
{
    [RequireComponent(typeof(Collider))]
    public class PathTeleporter : MonoBehaviour
    {
        private static WaitForSeconds _waitForSeconds0_1 = new WaitForSeconds(0.1f);
        [Header("Destination")]
        [Tooltip("All paths this teleporter can send the target to. The path matching the target's current Z is excluded, then one of the rest is picked at random.")]
        [SerializeField] private PathDefinition[] possiblePaths;

        [Header("Filter")]
        [SerializeField] private LayerMask targetMask;

        private readonly List<PathDefinition> _candidateBuffer = new List<PathDefinition>();

        private void OnTriggerEnter(Collider other) => TryTeleport(other.gameObject);
        private void OnCollisionEnter(Collision collision) => TryTeleport(collision.gameObject);

        private void TryTeleport(GameObject target)
        {
            if (!IsInLayerMask(target.layer, targetMask)) return;

            var rb = target.GetComponentInParent<Rigidbody>();
            if (rb == null) return;

            PathDefinition destination = PickDestination(rb.position.z);
            if (destination == null) return;

            StartCoroutine(TeleportZRoutine(rb, destination.zPosition));
        }

        private IEnumerator TeleportZRoutine(Rigidbody rb, float z)
        {
            RigidbodyConstraints originalConstraints = rb.constraints;
            bool hadZConstraint = (originalConstraints & RigidbodyConstraints.FreezePositionZ) != 0;

            if (hadZConstraint)
            {
                rb.constraints = originalConstraints & ~RigidbodyConstraints.FreezePositionZ;
            }

            Vector3 newPosition = rb.position;
            newPosition.z = z;
            rb.position = newPosition;
            Physics.SyncTransforms();

            yield return _waitForSeconds0_1;

            rb.position = newPosition;
            if (hadZConstraint)
            {
                rb.constraints = originalConstraints;
            }
        }

        private PathDefinition PickDestination(float currentZ)
        {
            _candidateBuffer.Clear();

            foreach (var path in possiblePaths)
            {
                if (path == null) continue;
                if (Mathf.Approximately(path.zPosition, currentZ)) continue;

                _candidateBuffer.Add(path);
            }

            if (_candidateBuffer.Count == 0) return null;

            return _candidateBuffer[Random.Range(0, _candidateBuffer.Count)];
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        private void OnDrawGizmos()
        {
            if (possiblePaths == null) return;

            float spacing = 0.4f;
            int index = 0;

            foreach (var path in possiblePaths)
            {
                if (path == null) continue;

                Gizmos.color = path.pathColor;
                Gizmos.DrawWireSphere(transform.position + new Vector3(0f, index * spacing, 0f), 0.2f);
                index++;
            }
        }
    }
}