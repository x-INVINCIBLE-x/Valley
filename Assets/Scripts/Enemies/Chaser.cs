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
        [Tooltip("Normal movement speed.")]
        [SerializeField] private float baseSpeed = 4f;

        [Tooltip("Speed used when the target is far ahead.")]
        [SerializeField] private float catchUpSpeed = 8f;

        [Tooltip("Speed used when the target is close ahead.")]
        [SerializeField] private float closeSpeed = 2f;

        [Header("Distance Thresholds")]
        [Tooltip("Distance beyond which catch-up speed is used.")]
        [SerializeField] private float catchUpDistance = 12f;

        [Tooltip("Distance below which close speed is used.")]
        [SerializeField] private float closeDistance = 3f;

        [Header("Ramping")]
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;

        private Rigidbody _rb;

        [SerializeField]
        private float _currentSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            _rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationY;

            _currentSpeed = baseSpeed;
        }

        private void FixedUpdate()
        {
            if (target == null)
            {
                _currentSpeed = Mathf.MoveTowards(
                    _currentSpeed,
                    baseSpeed,
                    deceleration * Time.fixedDeltaTime);

                _rb.linearVelocity = new Vector3(
                    _currentSpeed,
                    _rb.linearVelocity.y,
                    0f);

                return;
            }

            float deltaX = target.position.x - _rb.position.x;

            float desiredSpeed;

            if (deltaX <= 0f)
            {
                desiredSpeed = baseSpeed;
            }
            else
            {
                if (deltaX > catchUpDistance)
                {
                    desiredSpeed = catchUpSpeed;
                }
                else if (deltaX < closeDistance)
                {
                    desiredSpeed = closeSpeed;
                }
                else
                {
                    desiredSpeed = baseSpeed;
                }
            }

            float rate = desiredSpeed > _currentSpeed
                ? acceleration
                : deceleration;

            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed,
                desiredSpeed,
                rate * Time.fixedDeltaTime);

            float verticalVelocity =
                (target.position.y - _rb.position.y) / Time.fixedDeltaTime;

            float zVelocity = (target.position.z - _rb.position.z) / Time.fixedDeltaTime;

            _rb.linearVelocity = new Vector3(
                _currentSpeed,
                verticalVelocity,
                zVelocity);
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