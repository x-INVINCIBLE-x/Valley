using UnityEngine;

namespace Valley.Utility
{
    /// <summary>
    /// Continuously rotates this object's transform around a given local axis at a fixed speed.
    /// </summary>
    public class ContinuousRotator : MonoBehaviour
    {
        [Tooltip("Axis to rotate around, in local space.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float degreesPerSecond = 90f;

        [Tooltip("If true, uses unscaled time (ignores Time.timeScale).")]
        [SerializeField] private bool useUnscaledTime = false;

        private void Update()
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(rotationAxis.normalized, degreesPerSecond * deltaTime, Space.Self);
        }
    }
    }