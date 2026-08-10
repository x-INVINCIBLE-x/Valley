using UnityEngine;

namespace Valley.Level.Obstacles
{
    /// <summary>
    /// Global obstacle that spawns near the player, briefly telegraphs, then actively pursues them by
    /// closing the straight-line distance to the player's current position every frame. It pauses in
    /// place for as long as the player is inside its collider, and despawns the moment the distance to
    /// the player reaches despawnDistance, whether that's because the player pulled away or because it
    /// overshot past them.
    ///
    /// Reads player.position live each frame rather than caching or diffing it across frames, so unlike
    /// PlatformChunkSpawner this needs no WorldShiftEvents handling: whether the player moves or the
    /// world/platforms shift instead, the live relative vector between this and the player is correct
    /// either way.
    /// </summary>
    public class ChaserObstacle : ObstacleEntity
    {
        [Header("Visuals & Hitbox")]
        [Tooltip("Shown during anticipation - the spawn telegraph, not damaging yet.")]
        public GameObject anticipationVisual;
        [Tooltip("Shown while actively chasing.")]
        public GameObject actionVisual;

        [Header("Timing")]
        [Tooltip("Spawn telegraph duration before it starts chasing.")]
        public float anticipationDuration = 0.3f;

        [Header("Placement")]
        [Tooltip("X offset from the player's position at spawn time. A random value is picked from this range.")]
        public Vector2 spawnXOffsetRange = new Vector2(-4f, -4f);
        [Tooltip("Y offset from the player's position at spawn time. A random value is picked from this range.")]
        public Vector2 spawnYOffsetRange = new Vector2(0f, 0f);

        [Header("Chase")]
        [Tooltip("How fast this closes the straight-line distance to the player's current position.")]
        public float chaseSpeed = 8f;
        [Tooltip("Despawns once the straight-line distance to the player reaches this value.")]
        public float despawnDistance = 15f;

        [Header("Player Detection")]
        [Tooltip("Requires a trigger collider on this GameObject. Only objects on these layers count as the player for pausing movement while overlapped.")]
        public LayerMask playerLayerMask = ~0;

        enum Phase { Anticipation, Chasing }

        Phase phase;
        float phaseTimer;
        bool playerInsideCollider;

        public override void BeginAnticipation()
        {
            playerInsideCollider = false;

            if (player != null)
            {
                float xOffset = Random.Range(spawnXOffsetRange.x, spawnXOffsetRange.y);
                float yOffset = Random.Range(spawnYOffsetRange.x, spawnYOffsetRange.y);
                transform.position = new Vector3(player.position.x + xOffset, player.position.y + yOffset, transform.position.z);
            }

            SetPhase(Phase.Anticipation);
        }

        void Update()
        {
            switch (phase)
            {
                case Phase.Anticipation:
                    phaseTimer += Time.deltaTime;
                    if (phaseTimer >= anticipationDuration) SetPhase(Phase.Chasing);
                    break;
                case Phase.Chasing:
                    UpdateChase();
                    break;
            }
        }

        void UpdateChase()
        {
            if (player == null)
            {
                RequestDespawn();
                return;
            }

            Vector3 toPlayer = player.position - transform.position;
            float distance = toPlayer.magnitude;

            if (distance >= despawnDistance)
            {
                RequestDespawn();
                return;
            }

            if (playerInsideCollider) return;

            Vector3 direction = distance > 0.0001f ? toPlayer / distance : Vector3.zero;
            transform.position += direction * chaseSpeed * Time.deltaTime;
        }

        void SetPhase(Phase next)
        {
            phase = next;
            phaseTimer = 0f;

            if (anticipationVisual != null) anticipationVisual.SetActive(next == Phase.Anticipation);
            if (actionVisual != null) actionVisual.SetActive(next == Phase.Chasing);
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other)) playerInsideCollider = true;
        }

        bool IsPlayer(Collider other)
        {
            if (player == null) return false;
            if (other.transform != player && !other.transform.IsChildOf(player)) return false;
            return (playerLayerMask.value & (1 << other.gameObject.layer)) != 0;
        }
    }
}