using System;
using UnityEngine;
using Valley.Aiming;
using Valley.Player;

namespace Valley.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class SurfaceBounceHandler : MonoBehaviour
    {
        public static event Action<Vector3, Vector3, int> OnSurfaceBounce;

        [SerializeField] private LayerMask bounceMask;
        [SerializeField] private float baseBounceMultiplier = 0.95f;
        [SerializeField] private float minBounceSpeed = 2f;
        [SerializeField] private PlayerPlatformEffects platformEffects;

        private Rigidbody _rb;
        private int _bounceCount;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        private void OnEnable() => AimInputController.OnAimReleased += HandleAimReleased;
        private void OnDisable() => AimInputController.OnAimReleased -= HandleAimReleased;

        private void HandleAimReleased(Vector3 direction, float charge) => _bounceCount = 0;

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsInLayerMask(collision.gameObject.layer, bounceMask)) return;

            ContactPoint contact = collision.GetContact(0);
            Vector3 incoming = _rb.linearVelocity;

            float bounceMultiplier = platformEffects != null && platformEffects.Current != null
                ? platformEffects.Current.bounceMultiplier
                : baseBounceMultiplier;

            if (bounceMultiplier <= 0f)
            {
                _rb.linearVelocity = Vector3.ProjectOnPlane(incoming, contact.normal);
                return;
            }

            float normalSpeed = Vector3.Dot(incoming, contact.normal);
            if (Mathf.Abs(normalSpeed) < minBounceSpeed) return;

            Vector3 tangential = incoming - contact.normal * normalSpeed;
            Vector3 bounceVelocity = tangential + contact.normal * (Mathf.Abs(normalSpeed) * bounceMultiplier);
            _rb.linearVelocity = bounceVelocity;

            _bounceCount++;
            OnSurfaceBounce?.Invoke(contact.point, contact.normal, _bounceCount);
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    }
}