using System;
using UnityEngine;

namespace Valley.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerVerticalWrap : MonoBehaviour
    {
        public static event Action<Vector3> OnWrapped;

        [SerializeField] private float wrapBelowY = -10f;
        [SerializeField] private float wrapToY = 10f;
        [SerializeField] private bool preserveVelocityOnWrap = true;

        private Rigidbody _rb;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        private void FixedUpdate()
        {
            if (_rb.position.y > wrapBelowY) return;

            Vector3 wrappedPosition = _rb.position;
            wrappedPosition.y = wrapToY;
            _rb.position = wrappedPosition;

            if (!preserveVelocityOnWrap)
            {
                _rb.linearVelocity = Vector3.zero;
            }

            OnWrapped?.Invoke(wrappedPosition);
        }
    }
}