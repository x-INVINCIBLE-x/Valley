using UnityEngine;
using Valley.Aiming;

namespace Valley.Player
{
    public class AimArrowIndicator : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform arrowVisual;
        [SerializeField] private float orbitRadius = 1.2f;

        private void Awake()
        {
            if (arrowVisual == null) arrowVisual = transform;
            SetVisible(false);
        }

        private void OnEnable()
        {
            AimInputController.OnAimStarted += HandleAimStarted;
            AimInputController.OnAiming += HandleAiming;
            AimInputController.OnAimReleased += HandleAimReleased;
            AimInputController.OnAimCancelled += HandleAimCancelled;
        }

        private void OnDisable()
        {
            AimInputController.OnAimStarted -= HandleAimStarted;
            AimInputController.OnAiming -= HandleAiming;
            AimInputController.OnAimReleased -= HandleAimReleased;
            AimInputController.OnAimCancelled -= HandleAimCancelled;
        }

        private void HandleAimStarted() => SetVisible(true);

        private void HandleAiming(Vector3 direction, float charge)
        {
            if (player == null) return;

            arrowVisual.position = player.position + direction * orbitRadius;

            float angleDeg = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arrowVisual.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        private void HandleAimReleased(Vector3 direction, float charge) => SetVisible(false);

        private void HandleAimCancelled() => SetVisible(false);

        private void SetVisible(bool visible) => arrowVisual.gameObject.SetActive(visible);
    }
}