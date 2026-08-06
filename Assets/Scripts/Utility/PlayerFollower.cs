using UnityEngine;

namespace Valley
{
    public class PlayerFollower : MonoBehaviour
    {
        [SerializeField] private Vector3 offset;
        [SerializeField] private bool smoothFollow = true;
        [SerializeField] private float followSpeed = 10f;

        private void LateUpdate()
        {
            Transform player = PlayerManager.Instance?.PlayerTransform;

            if (player == null)
                return;

            Vector3 targetPosition = player.position + offset;

            if (smoothFollow)
            {
                float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, targetPosition, t);
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }
}