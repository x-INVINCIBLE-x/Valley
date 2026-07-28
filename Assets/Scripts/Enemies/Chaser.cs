using UnityEngine;

namespace Valley.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    public class Chaser : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The chaser follows this transform - normally the player.")]
        [SerializeField] private Transform target;

        [Header("Speeds")]
        [Tooltip("Speed used when the horizontal distance to target is between closeDistance and catchUpDistance.")]
        [SerializeField] private float baseSpeed = 4f;
        [Tooltip("Horizontal speed used when the chaser has fallen behind past catchUpDistance, to close the gap back down.")]
        [SerializeField] private float catchUpSpeed = 8f;
        [Tooltip("Horizontal speed used when the chaser is within closeDistance of the target, to ease off rather than ram at full speed.")]
        [SerializeField] private float closeSpeed = 2f;

        [Header("Distance Thresholds")]
        [Tooltip("Horizontal distance beyond which the chaser is considered left behind and switches to catchUpSpeed.")]
        [SerializeField] private float catchUpDistance = 12f;
        [Tooltip("Horizontal distance below which the chaser is considered close and switches to closeSpeed.")]
        [SerializeField] private float closeDistance = 3f;

        [Header("Ramping")]
        [Tooltip("How quickly current horizontal speed ramps up toward a higher desired speed, in units per second.")]
        [SerializeField] private float acceleration = 10f;
        [Tooltip("How quickly current horizontal speed ramps down toward a lower desired speed, in units per second. Y always tracks the target instantly and ignores both of these.")]
        [SerializeField] private float deceleration = 10f;

        private Rigidbody _rb;
        [SerializeField] private float _currentSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezePositionZ
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationY;
            _currentSpeed = baseSpeed;
        }

        private void FixedUpdate()
        {
            if (target == null) return;

            float horizontalDistance = Mathf.Abs(target.position.x - _rb.position.x);
            float desiredSpeed = DesiredSpeed(horizontalDistance);
            float rate = desiredSpeed > _currentSpeed ? acceleration : deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, desiredSpeed, rate * Time.fixedDeltaTime);

            float horizontalDirection = Mathf.Sign(target.position.x - _rb.position.x);
            if (horizontalDistance < 0.01f) horizontalDirection = 0f;

            float verticalVelocity = (target.position.y - _rb.position.y) / Time.fixedDeltaTime;

            _rb.linearVelocity = new Vector3(horizontalDirection * _currentSpeed, verticalVelocity, 0f);
        }

        private float DesiredSpeed(float distance)
        {
            if (distance > catchUpDistance) return catchUpSpeed;
            if (distance < closeDistance) return closeSpeed;
            return baseSpeed;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, closeDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, catchUpDistance);

            if (target != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}