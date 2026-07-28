using UnityEngine;

namespace Valley.Powerups
{
    [CreateAssetMenu(fileName = "LaunchBoostEffect", menuName = "Valley/Powerups/Launch Boost")]
    public class LaunchBoostEffect : PowerupEffect
    {
        [Header("Launch Boost")]
        [Tooltip("Speed added in the direction the pickup's arrow points.")]
        [SerializeField] private float speed = 15f;

        public override void Apply(GameObject target, Transform source)
        {
            var rb = target.GetComponent<Rigidbody>();
            if (rb == null) return;

            Vector3 direction = source != null ? source.right : Vector3.right;
            rb.linearVelocity = direction.normalized * speed;
        }
    }
}