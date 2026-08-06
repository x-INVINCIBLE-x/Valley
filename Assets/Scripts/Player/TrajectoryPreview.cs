using System.Collections.Generic;
using UnityEngine;
using Valley.Aiming;
using Valley.Core;
using Valley.Platforms;

namespace Valley.Player
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] private LaunchProfile profile;
        [SerializeField] private Rigidbody playerRigidbody;
        [SerializeField] private PlayerGravity playerGravity;
        [SerializeField] private LayerMask surfaceMask;

        [SerializeField] private int stepCount = 60;
        [SerializeField] private float stepTime = 0.03f;
        [SerializeField] private float castRadius = 0.1f;
        [SerializeField] private int maxPreviewBounces = 3;
        [SerializeField] private float baseBounceMultiplier = 1f;

        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 0;
        }

        private void OnEnable()
        {
            InputController.OnAimStarted += Show;
            InputController.OnAiming += UpdatePreview;
            InputController.OnAimReleased += HandleAimReleased;
            InputController.OnAimCancelled += Hide;
        }

        private void OnDisable()
        {
            InputController.OnAimStarted -= Show;
            InputController.OnAiming -= UpdatePreview;
            InputController.OnAimReleased -= HandleAimReleased;
            InputController.OnAimCancelled -= Hide;
        }

        private void Show() => _line.enabled = true;

        private void HandleAimReleased(Vector3 direction, float charge) => Hide();

        private void Hide()
        {
            _line.enabled = false;
            _line.positionCount = 0;
        }

        private void UpdatePreview(Vector3 direction, float charge)
        {
            if (direction == Vector3.zero || playerRigidbody == null || profile == null)
            {
                _line.positionCount = 0;
                return;
            }

            List<Vector3> points = Simulate(direction, charge);
            _line.positionCount = points.Count;
            _line.SetPositions(points.ToArray());
        }

        private List<Vector3> Simulate(Vector3 direction, float charge)
        {
            var points = new List<Vector3>(stepCount) { playerRigidbody.position };

            Vector3 pos = playerRigidbody.position;
            Vector3 launchVelocity = direction * (profile.EvaluateForce(charge) / Mathf.Max(playerRigidbody.mass, 0.01f));
            Vector3 current = playerRigidbody.linearVelocity;
            Vector3 velocity = new Vector3(
                current.x * profile.previousVelocityRetention + launchVelocity.x,
                launchVelocity.y,
                current.z);

            float gravityScale = playerGravity != null ? playerGravity.CurrentGravityScale : 1f;
            Vector3 gravity = Physics.gravity * gravityScale;

            int bounces = 0;

            for (int i = 0; i < stepCount; i++)
            {
                Vector3 nextPos = pos + velocity * stepTime;
                velocity += gravity * stepTime;

                float travel = Vector3.Distance(pos, nextPos);
                if (travel <= 0f)
                {
                    points.Add(nextPos);
                    pos = nextPos;
                    continue;
                }

                if (Physics.SphereCast(pos, castRadius, (nextPos - pos).normalized, out RaycastHit hit, travel, surfaceMask))
                {
                    points.Add(hit.point);

                    var zone = hit.collider.GetComponent<PlatformEffectZone>();
                    float bounceMultiplier = zone != null && zone.Profile != null ? zone.Profile.bounceMultiplier : baseBounceMultiplier;

                    if (bounceMultiplier <= 0f)
                    {
                        velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
                        pos = hit.point + hit.normal * 0.02f;
                        continue;
                    }

                    if (bounces >= maxPreviewBounces) break;

                    velocity = Vector3.Reflect(velocity, hit.normal) * bounceMultiplier;
                    pos = hit.point + hit.normal * 0.02f;
                    bounces++;
                    continue;
                }

                points.Add(nextPos);
                pos = nextPos;
            }

            return points;
        }
    }
}