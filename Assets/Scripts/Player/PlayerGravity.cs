using UnityEngine;

namespace Valley.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerGravity : MonoBehaviour
    {
        [SerializeField] private float baseGravityScale = 1f;
        [SerializeField] private PlayerPlatformEffects platformEffects;

        private Rigidbody _rb;

        public float CurrentGravityScale =>
            baseGravityScale * (platformEffects != null && platformEffects.Current != null ? platformEffects.Current.gravityMultiplier : 1f);

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
        }

        private void FixedUpdate()
        {
            _rb.AddForce(Physics.gravity * CurrentGravityScale, ForceMode.Acceleration);
        }
    }
}
