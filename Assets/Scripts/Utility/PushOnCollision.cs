using UnityEngine;

public class PushOnCollision : MonoBehaviour
{
    [SerializeField] private float pushForce = 10f;
    [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.TryGetComponent(out Rigidbody rb))
        {
            Vector3 force = transform.right * pushForce;
            rb.AddForce(force, forceMode);
        }
    }
}