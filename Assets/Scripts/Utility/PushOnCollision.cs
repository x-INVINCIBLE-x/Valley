using UnityEngine;

public class PushOnCollision : MonoBehaviour
{
    [SerializeField] private float pushForce = 10f;
    [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.TryGetComponent(out Rigidbody rb))
        {
            Debug.Log($"Pushing {collision.transform.name} with force {pushForce}");
            Vector3 force = transform.right * pushForce;
            rb.AddForce(force, forceMode);
        }
    }
}