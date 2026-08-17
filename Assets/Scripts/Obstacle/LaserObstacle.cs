using UnityEngine;
using Valley.Combat;

namespace Valley.Level.Obstacles
{
    /// <summary>
    /// Jetpack Joyride style laser obstacle.
    /// - Telegraph phase: beam(s) extend from the shared start point out to each end point.
    /// - Action phase: beam(s) become damaging.
    /// - Recovery phase: beam(s) turn off and despawn.
    /// Supports multiple laser ends fanning out from a single start point (e.g. a hub with
    /// several spokes), each with its own deploy offset and one or more LineRenderers.
    /// </summary>
    public class LaserObstacle : ObstacleEntity
    {
        [System.Serializable]
        private class LaserBeam
        {
            public Transform end;

            [Tooltip("All LineRenderers that should visually track this beam (e.g. core + glow passes).")]
            public LineRenderer[] lines;

            [Tooltip("Local-space offset this end moves by during the anticipation/deploy phase.")]
            public Vector3 deployOffset = new Vector3(-2f, 0f, 0f);

            [HideInInspector] public Vector3 initialLocalPos;
        }

        [Header("Visuals & Hitbox")]
        [SerializeField] private GameObject anticipationVisual;
        [SerializeField] private Transform laserStart;
        [SerializeField] private LaserBeam[] laserEnds;
        [SerializeField] private GameObject actionVisual;
        [SerializeField] private Collider[] damageColliders;

        [Header("Timing")]
        [SerializeField] private float anticipationDuration = 0.6f;
        [SerializeField] private float actionDuration = 0.4f;
        [SerializeField] private float recoveryDuration = 0.2f;

        [Header("Beam Deploy")]
        [Tooltip("How far the start box moves on the X axis during anticipation.")]
        [SerializeField] private float startBoxXOffset = 2f;

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

        private void Awake()
        {
            laserStartInitialLocalPos = laserStart.localPosition;

            foreach (LaserBeam beam in laserEnds)
            {
                if (beam.end != null)
                    beam.initialLocalPos = beam.end.localPosition;
            }
        }

        public override void BeginAnticipation()
        {
            if (IsPositionRoot)
                placement.PlaceNearPlayer(transform, player);

            laserStart.localPosition = laserStartInitialLocalPos;

            foreach (LaserBeam beam in laserEnds)
            {
                if (beam.end != null)
                    beam.end.localPosition = beam.initialLocalPos;
            }

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

                laserStart.localPosition = Vector3.Lerp(
                    laserStartInitialLocalPos,
                    startTarget,
                    t);

                foreach (LaserBeam beam in laserEnds)
                {
                    if (beam.end == null)
                        continue;

                    Vector3 endTarget = beam.initialLocalPos + beam.deployOffset;
                    beam.end.localPosition = Vector3.Lerp(beam.initialLocalPos, endTarget, t);
                }
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

            float t = phase == Phase.Anticipation
                ? Mathf.Clamp01(phaseTimer / (beamDeployDuration > 0f ? beamDeployDuration : anticipationDuration))
                : 1f;

            foreach (LaserBeam beam in laserEnds)
            {
                if (beam.end == null || beam.lines == null)
                    continue;

                Vector3 currentEnd = Vector3.Lerp(start, beam.end.position, t);

                foreach (LineRenderer line in beam.lines)
                {
                    if (line == null)
                        continue;

                    line.positionCount = 2;
                    line.SetPosition(0, start);
                    line.SetPosition(1, currentEnd);
                }
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

                foreach (LaserBeam beam in laserEnds)
                {
                    if (beam.end != null)
                        beam.end.localPosition = beam.initialLocalPos + beam.deployOffset;
                }
            }

            anticipationVisual?.SetActive(next == Phase.Anticipation);
            actionVisual?.SetActive(next == Phase.Action);

            foreach (Collider collider in damageColliders)
            {
                if (collider != null)
                    collider.enabled = next == Phase.Action;
            }
        }
    }
}