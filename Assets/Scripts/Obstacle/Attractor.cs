using UnityEngine;

namespace Valley.Obstacle
{
    public class Attractor : MonoBehaviour
    {
        [Header("Range")]
        [Tooltip("Objects on this layer within radius get pulled.")]
        [SerializeField] private LayerMask targetMask;
        [Tooltip("Objects beyond this distance are not affected.")]
        [SerializeField] private float radius = 10f;

        [Header("Strength")]
        [Tooltip("Pull strength at the very center (distance = 0).")]
        [SerializeField] private float maxStrength = 20f;
        [Tooltip("X = normalized distance from center (0 = at center, 1 = at radius edge). Y = strength multiplier at that distance. Default falls from full strength at the center to zero at the edge.")]
        [SerializeField] private AnimationCurve strengthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private readonly Collider[] _overlapBuffer = new Collider[32];

        private void FixedUpdate()
        {
            int count = UnityEngine.Physics.OverlapSphereNonAlloc(transform.position, radius, _overlapBuffer, targetMask);

            for (int i = 0; i < count; i++)
            {
                Pull(_overlapBuffer[i]);
            }
        }

        private void Pull(Collider target)
        {
            Rigidbody rb = target.attachedRigidbody;
            if (rb == null) return;

            Vector3 toCenter = transform.position - rb.position;
            toCenter.z = 0f;

            float distance = toCenter.magnitude;
            if (distance < 0.01f || distance > radius) return;

            float normalizedDistance = distance / radius;
            float strength = maxStrength * strengthCurve.Evaluate(normalizedDistance);

            Vector3 direction = toCenter / distance;
            rb.AddForce(direction * strength, ForceMode.Acceleration);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}