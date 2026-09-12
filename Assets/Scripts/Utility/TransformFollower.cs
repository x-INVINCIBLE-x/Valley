using UnityEngine;

namespace Valley.Core
{
    public class TransformFollower : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Axes")]
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY = true;
        [SerializeField] private bool followZ = true;

        [Header("Position")]
        [SerializeField] private Vector3 offset;

        [Header("Smoothing")]
        [SerializeField] private bool smooth;
        [SerializeField, Min(0f)] private float followSpeed = 5f;

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 targetPosition = target.position + offset;
            Vector3 currentPosition = transform.position;

            if (!followX)
                targetPosition.x = currentPosition.x;

            if (!followY)
                targetPosition.y = currentPosition.y;

            if (!followZ)
                targetPosition.z = currentPosition.z;

            if (smooth)
            {
                transform.position = Vector3.Lerp(
                    currentPosition,
                    targetPosition,
                    followSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = targetPosition;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}