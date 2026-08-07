using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Valley.Paths
{
    [RequireComponent(typeof(Collider))]
    public class PathTeleporter : MonoBehaviour
    {
        [Header("Destination")]
        [Tooltip("All paths this teleporter can send the target to. The path matching the target's current Z is excluded, then one of the rest is picked at random.")]
        [SerializeField] private PathDefinition[] possiblePaths;

        [Header("Movement")]
        [Tooltip("How fast Z moves toward the destination, in units per second.")]
        [SerializeField] private float zTransitionSpeed = 10f;

        [Tooltip("Maximum duration before force-finishing the transition.")]
        [SerializeField] private float maxTransitionDuration = 2f;

        [Header("Filter")]
        [SerializeField] private LayerMask targetMask;

        private readonly List<PathDefinition> _candidateBuffer = new();

        private static readonly HashSet<Rigidbody> InTransitionGlobal = new();

        private void OnTriggerEnter(Collider other) => TryTeleport(other.gameObject);

        private void OnCollisionEnter(Collision collision) => TryTeleport(collision.gameObject);

        private void TryTeleport(GameObject target)
        {
            if (!IsInLayerMask(target.layer, targetMask))
                return;

            Rigidbody rb = target.GetComponentInParent<Rigidbody>();

            if (rb == null || InTransitionGlobal.Contains(rb))
                return;

            PathDefinition destination = PickDestination(rb.position.z);

            if (destination == null)
                return;

            StartCoroutine(MoveZRoutine(rb, destination.zPosition));
        }

        private IEnumerator MoveZRoutine(Rigidbody rb, float targetZ)
        {
            InTransitionGlobal.Add(rb);

            bool originalKinematic = rb.isKinematic;
            RigidbodyConstraints originalConstraints = rb.constraints;

            try
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                float elapsed = 0f;

                while (Mathf.Abs(rb.position.z - targetZ) > 0.01f &&
                       elapsed < maxTransitionDuration)
                {
                    Vector3 position = rb.position;
                    position.z = Mathf.MoveTowards(
                        position.z,
                        targetZ,
                        zTransitionSpeed * Time.fixedDeltaTime);

                    rb.MovePosition(position);

                    elapsed += Time.fixedDeltaTime;
                    yield return new WaitForFixedUpdate();
                }

                Vector3 finalPosition = rb.position;
                finalPosition.z = targetZ;
                rb.position = finalPosition;

                Physics.SyncTransforms();
            }
            finally
            {
                rb.constraints = originalConstraints;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                InTransitionGlobal.Remove(rb);
            }
        }

        private PathDefinition PickDestination(float currentZ)
        {
            _candidateBuffer.Clear();

            foreach (PathDefinition path in possiblePaths)
            {
                if (path == null)
                    continue;

                if (Mathf.Approximately(path.zPosition, currentZ))
                    continue;

                _candidateBuffer.Add(path);
            }

            if (_candidateBuffer.Count == 0)
                return null;

            return _candidateBuffer[Random.Range(0, _candidateBuffer.Count)];
        }

        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void OnDrawGizmos()
        {
            if (possiblePaths == null)
                return;

            float spacing = 0.4f;
            int index = 0;

            foreach (PathDefinition path in possiblePaths)
            {
                if (path == null)
                    continue;

                Gizmos.color = path.pathColor;
                Gizmos.DrawWireSphere(
                    transform.position + new Vector3(0f, index * spacing, 0f),
                    0.2f);

                index++;
            }
        }
    }
}