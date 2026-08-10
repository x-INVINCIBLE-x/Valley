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
        [Tooltip("How long the Z transition takes, in seconds. X/Y motion continues unaffected during this time.")]
        [SerializeField] private float zTransitionDuration = 0.35f;

        [Tooltip("Optional easing curve for the Z lerp. Leave empty/linear for a constant-speed transition.")]
        [SerializeField] private AnimationCurve zEasing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

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

            // Capture momentum BEFORE touching the rigidbody at all.
            // This is what gets preserved through the transition and handed back at the end.
            Vector3 conservedVelocity = rb.linearVelocity;
            Vector3 conservedAngularVelocity = rb.angularVelocity;
            bool originalKinematic = rb.isKinematic;
            RigidbodyConstraints originalConstraints = rb.constraints;

            // Go kinematic so we have full deterministic control of position for the
            // duration of the transition, instead of fighting gravity/collision response
            // while also trying to drive Z manually.
            rb.isKinematic = true;

            // FreezePositionZ (used to keep the player locked to its lane during normal
            // play) blocks MovePosition/position writes on Z even while kinematic, so it
            // has to come off for the duration of the transition.
            rb.constraints = originalConstraints & ~RigidbodyConstraints.FreezePositionZ;

            float startZ = rb.position.z;
            float elapsed = 0f;
            float duration = Mathf.Max(zTransitionDuration, 0.0001f);

            try
            {
                while (elapsed < duration)
                {
                    elapsed += Time.fixedDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float easedT = zEasing != null && zEasing.length > 0 ? zEasing.Evaluate(t) : t;

                    Vector3 position = rb.position;

                    // X/Y keep moving exactly as if physics were still simulating them,
                    // using the velocity we captured at the start.
                    position.x += conservedVelocity.x * Time.fixedDeltaTime;
                    position.y += conservedVelocity.y * Time.fixedDeltaTime;
                    position.z = Mathf.Lerp(startZ, targetZ, easedT);

                    rb.MovePosition(position);

                    yield return new WaitForFixedUpdate();
                }

                Vector3 finalPosition = rb.position;
                finalPosition.z = targetZ;
                rb.position = finalPosition;
            }
            finally
            {
                rb.isKinematic = originalKinematic;
                rb.constraints = originalConstraints;

                // Hand the original momentum back so the player exits the teleport
                // carrying the same X/Y velocity (and spin) they entered with.
                rb.linearVelocity = conservedVelocity;
                rb.angularVelocity = conservedAngularVelocity;

                Physics.SyncTransforms();

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