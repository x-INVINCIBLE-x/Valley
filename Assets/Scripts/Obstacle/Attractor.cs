using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Valley.Obstacle
{
    [RequireComponent(typeof(SphereCollider))]
    public class Attractor : MonoBehaviour
    {
        [Header("Range")]
        [Tooltip("Objects on this layer within radius get pulled.")]
        [SerializeField] private LayerMask targetMask;
        [Tooltip("Objects beyond this distance are not affected. Also drives the trigger SphereCollider's radius - keep 'Is Trigger' checked on it.")]
        [SerializeField] private float radius = 10f;

        [Header("Strength")]
        [Tooltip("Pull strength at the very center (distance = 0).")]
        [SerializeField] private float maxStrength = 20f;
        [Tooltip("X = normalized distance from center (0 = at center, 1 = at radius edge). Y = strength multiplier at that distance. Default falls from full strength at the center to zero at the edge.")]
        [SerializeField] private AnimationCurve strengthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [SerializeField] private MMF_Player activateFeedback;
        [SerializeField] private MMF_Player deactivateFeedback;

        // Refcounted per rigidbody so a target with multiple colliders on targetMask doesn't get
        // dropped the moment just ONE of its colliders exits while another is still inside.
        private readonly Dictionary<Rigidbody, int> _tracked = new();
        private readonly List<Rigidbody> _pruneBuffer = new();

        private SphereCollider _triggerCollider;
        private bool active = false;

        private void Awake()
        {
            _triggerCollider = GetComponent<SphereCollider>();
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = radius;
        }

        private void OnValidate()
        {
            if (_triggerCollider == null) _triggerCollider = GetComponent<SphereCollider>();
            if (_triggerCollider != null) _triggerCollider.radius = radius;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsInLayerMask(other.gameObject.layer, targetMask)) return;

            Rigidbody rb = other.attachedRigidbody;
            if (rb == null) return;

            _tracked.TryGetValue(rb, out int count);
            _tracked[rb] = count + 1;

            if (!active)
                activateFeedback?.PlayFeedbacks();
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || !_tracked.TryGetValue(rb, out int count)) return;

            if (count <= 1) _tracked.Remove(rb);
            else _tracked[rb] = count - 1;

            if (active && _tracked.Count == 0)
                deactivateFeedback?.PlayFeedbacks();
        }

        private void FixedUpdate()
        {
            if (_tracked.Count == 0) return;

            _pruneBuffer.Clear();

            foreach (var kvp in _tracked)
            {
                Rigidbody rb = kvp.Key;

                if (rb == null || !rb.gameObject.activeInHierarchy)
                {
                    _pruneBuffer.Add(rb);
                    continue;
                }

                Pull(rb);
            }

            if (_pruneBuffer.Count == 0) return;

            foreach (var rb in _pruneBuffer) _tracked.Remove(rb);
            if (active && _tracked.Count == 0) SetActive(false);
        }

        private void Pull(Rigidbody rb)
        {
            Vector3 toCenter = transform.position - rb.position;
            toCenter.z = 0f;

            float distance = toCenter.magnitude;
            if (distance < 0.01f || distance > radius) return;

            float normalizedDistance = distance / radius;
            float strength = maxStrength * strengthCurve.Evaluate(normalizedDistance);

            Vector3 direction = toCenter / distance;
            rb.AddForce(direction * strength, ForceMode.Acceleration);
        }

        private void SetActive(bool value)
        {
            if (active == value) return;
            active = value;

            if (value) activateFeedback?.PlayFeedbacks();
            else deactivateFeedback?.PlayFeedbacks();
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}