using MoreMountains.Feedbacks;
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
        [SerializeField] private float maxBounceSpeedForIntensity = 15f; // speed at which intensity hits 2
        [SerializeField] private PlayerPlatformEffects platformEffects;
        [SerializeField] private MMF_Player bounceFeedback;

        private Rigidbody _rb;
        private MMF_SquashAndStretch squashFeedback;

        private int _bounceCount;
        private float _baseSquash;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            squashFeedback = bounceFeedback.GetFeedbackOfType<MMF_SquashAndStretch>();

            if (squashFeedback != null)
            {
                _baseSquash = squashFeedback.RemapCurveOne;
            }
        }

        private void OnEnable() => InputController.OnAimReleased += HandleAimReleased;
        private void OnDisable() => InputController.OnAimReleased -= HandleAimReleased;

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

            if (squashFeedback != null)
            {
                float intensity = Mathf.Lerp(
                    1f, 1.5f,
                    Mathf.InverseLerp(minBounceSpeed, maxBounceSpeedForIntensity, bounceVelocity.magnitude)
                );
                Debug.Log($"Bounce intensity: {intensity}");
                squashFeedback.RemapCurveOne = _baseSquash * intensity;
                squashFeedback.Axis = GetAxisFromNormal(contact.normal);
            }

            bounceFeedback.PlayFeedbacks();
            OnSurfaceBounce?.Invoke(contact.point, contact.normal, _bounceCount);
        }

        private static MMF_SquashAndStretch.PossibleAxis GetAxisFromNormal(Vector3 normal)
        {
            float absX = Mathf.Abs(normal.x);
            float absY = Mathf.Abs(normal.y);
            float absZ = Mathf.Abs(normal.z);

            if (absX >= absY && absX >= absZ)
                return MMF_SquashAndStretch.PossibleAxis.YtoXZ;

            if (absY >= absX && absY >= absZ)
                return MMF_SquashAndStretch.PossibleAxis.XtoYZ;

            return MMF_SquashAndStretch.PossibleAxis.ZtoXY;
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
    }
}