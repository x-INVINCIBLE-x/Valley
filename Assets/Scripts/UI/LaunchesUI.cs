using MoreMountains.Feedbacks;
using UnityEngine;

public class LaunchesUI : MonoBehaviour
{
    [SerializeField] private MMF_Player onAimStarted;
    [SerializeField] private MMF_Player onAimReleased;
    [SerializeField] private MMF_Player onAimCancelled;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;

    private bool isRotating;
    private float rotationDirection;

    public void HandleAimStarted(bool clockwise)
    {
        rotationDirection = clockwise ? -1f : 1f;
        isRotating = true;

        onAimStarted?.PlayFeedbacks();
    }

    public void HandleAimReleased(Vector3 vector, float arg2)
    {
        StopRotation();

        onAimReleased?.PlayFeedbacks();
    }

    public void HandleAimCancelled()
    {
        StopRotation();

        onAimCancelled?.PlayFeedbacks();
    }

    private void Update()
    {
        if (!isRotating)
            return;

        transform.Rotate(
            0f,
            0f,
            rotationDirection * rotationSpeed * Time.unscaledDeltaTime
        );
    }

    private void StopRotation()
    {
        isRotating = false;
        rotationDirection = 0f;
    }
}