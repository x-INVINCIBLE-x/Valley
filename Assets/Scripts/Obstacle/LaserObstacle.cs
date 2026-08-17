using UnityEngine;
using Valley.Combat;

namespace Valley.Level.Obstacles
{
    /// <summary>
    /// Jetpack Joyride style laser obstacle.
    /// - Telegraph phase: beam extends from both ends.
    /// - Action phase: beam becomes damaging.
    /// - Recovery phase: beam turns off and despawns.
    /// </summary>
    public class LaserObstacle : ObstacleEntity
    {
        [Header("Visuals & Hitbox")]
        [SerializeField] private GameObject anticipationVisual;
        [SerializeField] private Transform laserStart;
        [SerializeField] private Transform laserEnd;
        [SerializeField] private GameObject actionVisual;
        [SerializeField] private Collider damageCollider;
        [SerializeField] private LineRenderer[] lines;

        [Header("Timing")]
        [SerializeField] private float anticipationDuration = 0.6f;
        [SerializeField] private float actionDuration = 0.4f;
        [SerializeField] private float recoveryDuration = 0.2f;

        [Header("Beam Deploy")]
        [Tooltip("How far the start box moves on the X axis during anticipation.")]
        [SerializeField] private float startBoxXOffset = 2f;

        [Tooltip("How far the end box moves on the X axis during anticipation.")]
        [SerializeField] private float endBoxXOffset = -2f;

        [Tooltip("Duration of the beam deployment. Leave at 0 to match anticipation duration.")]
        [SerializeField] private float beamDeployDuration = 0f;

        [Header("Placement")]
        [Tooltip("Handles spawn offset + follow-the-player behavior. Only runs if IsPositionRoot is true " +
                 "(a ParentObstacle sets this false on its children so only the parent moves).")]
        [SerializeField] private ObstacleSpawnPlacement placement = new();

        private enum Phase
        {
            Anticipation,
            Action,
            Recovery
        }

        private Phase phase;
        private float phaseTimer;

        private Vector3 laserStartInitialLocalPos;
        private Vector3 laserEndInitialLocalPos;

        private void Awake()
        {
            laserStartInitialLocalPos = laserStart.localPosition;
            laserEndInitialLocalPos = laserEnd.localPosition;
        }

        public override void BeginAnticipation()
        {
            if (IsPositionRoot)
                placement.PlaceNearPlayer(transform, player);

            laserStart.localPosition = laserStartInitialLocalPos;
            laserEnd.localPosition = laserEndInitialLocalPos;

            SetPhase(Phase.Anticipation);
        }

        private void Update()
        {
            if (IsPositionRoot)
                placement.UpdateFollow(transform, player);

            UpdatePhase();
        }

        private void UpdatePhase()
        {
            phaseTimer += Time.deltaTime;

            if (phase == Phase.Anticipation)
            {
                float duration = beamDeployDuration > 0f
                    ? beamDeployDuration
                    : anticipationDuration;

                float t = Mathf.Clamp01(phaseTimer / duration);

                Vector3 startTarget =
                    laserStartInitialLocalPos + Vector3.right * startBoxXOffset;

                Vector3 endTarget =
                    laserEndInitialLocalPos + Vector3.right * endBoxXOffset;

                laserStart.localPosition = Vector3.Lerp(
                    laserStartInitialLocalPos,
                    startTarget,
                    t);

                laserEnd.localPosition = Vector3.Lerp(
                    laserEndInitialLocalPos,
                    endTarget,
                    t);
            }

            switch (phase)
            {
                case Phase.Anticipation:
                    if (phaseTimer >= anticipationDuration)
                        SetPhase(Phase.Action);
                    break;

                case Phase.Action:
                    if (phaseTimer >= actionDuration)
                        SetPhase(Phase.Recovery);
                    break;

                case Phase.Recovery:
                    if (phaseTimer >= recoveryDuration)
                        RequestDespawn();
                    break;
            }
        }

        private void LateUpdate()
        {
            Vector3 start = laserStart.position;
            Vector3 end = laserEnd.position;

            float t = phase == Phase.Anticipation
                ? Mathf.Clamp01(phaseTimer / (beamDeployDuration > 0f ? beamDeployDuration : anticipationDuration))
                : 1f;

            Vector3 currentEnd = Vector3.Lerp(start, end, t);

            foreach (LineRenderer line in lines)
            {
                if (line == null)
                    continue;

                line.positionCount = 2;
                line.SetPosition(0, start);
                line.SetPosition(1, currentEnd);
            }
        }

        private void SetPhase(Phase next)
        {
            phase = next;
            phaseTimer = 0f;

            if (next == Phase.Action)
            {
                laserStart.localPosition =
                    laserStartInitialLocalPos + Vector3.right * startBoxXOffset;

                laserEnd.localPosition =
                    laserEndInitialLocalPos + Vector3.right * endBoxXOffset;
            }

            anticipationVisual?.SetActive(next == Phase.Anticipation);
            actionVisual?.SetActive(next == Phase.Action);

            if (damageCollider != null)
                damageCollider.enabled = next == Phase.Action;
        }
    }
}