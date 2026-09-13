using UnityEngine;

public class RotateWithVelocity : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

    [Header("Rotation")]
    [SerializeField] private float rotationMultiplier = 10f;
    [SerializeField] private Vector3 rotationAxis = Vector3.right;

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        Vector3 velocity = rb.linearVelocity;

        if (velocity.sqrMagnitude > 0.0001f)
        {
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, velocity.normalized);

            float rotationAmount =
                velocity.magnitude *
                rotationMultiplier *
                Time.fixedDeltaTime;

            transform.Rotate(
                rotationAxis,
                rotationAmount,
                Space.World
            );
        }
    }
} 