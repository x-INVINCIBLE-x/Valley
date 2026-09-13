using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private bool useUnscaledDeltaTime = false;

    private void Update()
    {
        float deltaTime = useUnscaledDeltaTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        transform.Rotate(
            rotationAxis.normalized,
            rotationSpeed * deltaTime,
            Space.Self
        );
    }
}