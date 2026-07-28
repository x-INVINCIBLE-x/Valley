using UnityEngine;
using Valley.Aiming;
using Valley.Core;

namespace Valley.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerLauncher : MonoBehaviour
    {
        [SerializeField] private LaunchProfile profile;
        [SerializeField] private PlayerPlatformEffects platformEffects;
        [SerializeField] private PlayerLaunchGate launchGate;

        private Rigidbody _rb;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        private void OnEnable() => AimInputController.OnAimReleased += Launch;
        private void OnDisable() => AimInputController.OnAimReleased -= Launch;

        private void Launch(Vector3 direction, float charge)
        {
            if (direction == Vector3.zero) return;
            if (launchGate != null && !launchGate.TryConsume()) return;

            float speedMultiplier = platformEffects != null && platformEffects.Current != null
                ? platformEffects.Current.speedMultiplier
                : 1f;

            float force = profile.EvaluateForce(charge) * speedMultiplier;
            _rb.linearVelocity *= profile.previousVelocityRetention;
            _rb.AddForce(direction * force, ForceMode.Impulse);
        }
    }
}