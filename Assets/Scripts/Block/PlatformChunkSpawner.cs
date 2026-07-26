using System.Collections.Generic;
using UnityEngine;
using Valley.Core;

namespace Valley.Level.Generation
{
    public class PlatformChunkSpawner : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        public PlatformBlock[] platformPrefabs;

        [Header("Reachability")]
        [Tooltip("Should mirror the player's actual forward-run speed.")]
        public float forwardSpeed = 6f;
        [Tooltip("The same LaunchProfile asset driving the player's launch motor.")]
        public LaunchProfile launchProfile;
        public float gravity = 20f;
        public int maxLaunches = 2;
        [Tooltip("Distance/height subtracted from the computed max reach so imperfect player timing still lands.")]
        public float reachabilitySafetyMargin = 0.5f;

        [Header("Spawn Window")]
        public float spawnAheadDistance = 30f;
        public float despawnBehindDistance = 15f;

        [Header("Vertical Band")]
        [Tooltip("Platforms never generate above (player's current Y + this offset). Re-evaluated on every spawn, so it tracks the player instead of trapping them under a fixed ceiling.")]
        public float upperBoundOffset = 6f;
        [Tooltip("Max |change in edge height| allowed between two consecutive platforms. There is deliberately no matching lower clamp.")]
        public float maxVerticalStep = 3f;

        [Header("Gap Control")]
        public float minGapX = 0.5f;
        [Tooltip("Authored ceiling on gap size; still further clamped by reachability.")]
        public float maxGapX = 4f;
        [Range(0f, 1f)] public float gapChance = 0.65f;

        [Header("Anti-Runaway Safety")]
        [Tooltip("Hard cap on flush attaches in a row, so blocks that allow sticking can't chain into an endless floor.")]
        public int maxConsecutiveSticks = 3;
        [Tooltip("After this many near-max-difficulty gaps in a row, the next gap eases off.")]
        public int maxConsecutiveHardGaps = 2;
        [Tooltip("Every N spawns, force a flat, unrotated, easy gap as a guaranteed-reachable checkpoint.")]
        public int guaranteedSafetyInterval = 8;

        [Header("Debug Gizmos")]
        public bool showDebugGizmos = true;
        public Color spawnAheadColor = new Color(0.2f, 1f, 0.4f);
        public Color despawnBehindColor = Color.red;
        public Color ceilingColor = Color.yellow;
        public Color lastEdgeColor = Color.magenta;
        public Color reachEnvelopeColor = new Color(1f, 0.5f, 0.1f, 0.8f);
        [Tooltip("Half-height of the vertical window markers drawn for the spawn-ahead/despawn-behind lines.")]
        public float gizmoLineHalfHeight = 4f;

        LaunchReachability.Envelope envelope;

        PlatformBlock lastPlatform;
        float lastRightEdgeX;
        float lastEdgeY;
        int consecutiveSticks;
        int consecutiveHardGaps;
        int spawnsSinceSafety;

        readonly Queue<PlatformBlock> active = new Queue<PlatformBlock>();
        readonly Dictionary<PlatformBlock, Queue<PlatformBlock>> pools = new Dictionary<PlatformBlock, Queue<PlatformBlock>>();
        readonly Dictionary<PlatformBlock, PlatformBlock> instanceSource = new Dictionary<PlatformBlock, PlatformBlock>();

        void Start()
        {
            envelope = LaunchReachability.Calculate(forwardSpeed, launchProfile, gravity, maxLaunches);
            SpawnInitial();
        }

        void Update()
        {
            if (player == null || platformPrefabs == null || platformPrefabs.Length == 0) return;

            while (player.position.x + spawnAheadDistance > lastRightEdgeX)
                SpawnNext();

            DespawnBehind();
        }

        void SpawnInitial()
        {
            PlatformBlock prefab = platformPrefabs[0];
            PlatformBlock instance = GetFromPool(prefab);
            float startLeftX = player.position.x - prefab.Width * 0.5f;
            float startLeftY = player.position.y - 0.1f;
            PositionPlatform(instance, startLeftX, startLeftY, 0f);

            lastPlatform = instance;
            lastRightEdgeX = instance.GetRightEdgeWorld().x;
            lastEdgeY = instance.GetRightEdgeWorld().y;
            active.Enqueue(instance);
        }

        void SpawnNext()
        {
            spawnsSinceSafety++;
            bool forceSafe = spawnsSinceSafety >= guaranteedSafetyInterval;

            PlatformBlock prefab = ChoosePrefab();

            bool wantsGap = forceSafe || Random.value < gapChance;
            bool canStick = !wantsGap && lastPlatform != null
                             && lastPlatform.rightAttach.allowed && prefab.leftAttach.allowed
                             && consecutiveSticks < maxConsecutiveSticks;
            bool stick = canStick && Random.value <= Mathf.Min(lastPlatform.rightAttach.successRate, prefab.leftAttach.successRate);

            float targetLeftEdgeY;
            float gapX;

            if (stick)
            {
                // flush attach: continue exactly at the previous block's edge height, zero gap
                targetLeftEdgeY = lastEdgeY;
                gapX = 0f;
                consecutiveSticks++;
                consecutiveHardGaps = 0;
            }
            else
            {
                consecutiveSticks = 0;

                float dynamicCeiling = player.position.y + upperBoundOffset;
                float minY = lastEdgeY - maxVerticalStep;              // no global lower bound, only a local step cap
                float maxY = Mathf.Min(lastEdgeY + maxVerticalStep, dynamicCeiling);
                if (maxY < minY) maxY = minY;

                float rawTarget = forceSafe ? lastEdgeY : Random.Range(minY, maxY);

                // never ask for more height than the launches can actually deliver, minus the forgiveness margin
                float maxReachableY = lastEdgeY + Mathf.Max(0f, envelope.maxUpwardHeight - reachabilitySafetyMargin);
                targetLeftEdgeY = Mathf.Min(rawTarget, maxReachableY);

                float heightDelta = targetLeftEdgeY - lastEdgeY;
                float safeGap = Mathf.Max(minGapX, ComputeSafeGap(heightDelta) - reachabilitySafetyMargin);
                float hardCap = forceSafe ? Mathf.Min(minGapX * 1.5f, safeGap) : Mathf.Min(maxGapX, safeGap);

                bool isHardGap = !forceSafe && hardCap >= safeGap * 0.85f;
                consecutiveHardGaps = isHardGap ? consecutiveHardGaps + 1 : 0;
                if (consecutiveHardGaps > maxConsecutiveHardGaps) hardCap *= 0.6f;

                gapX = Random.Range(minGapX, Mathf.Max(minGapX, hardCap));
            }

            float spawnLeftEdgeX = lastRightEdgeX + gapX;
            float rotationZ = (!forceSafe && prefab.rotation.allowRotation)
                ? Random.Range(prefab.rotation.minAngleDegrees, prefab.rotation.maxAngleDegrees)
                : 0f;

            PlatformBlock instance = GetFromPool(prefab);
            PositionPlatform(instance, spawnLeftEdgeX, targetLeftEdgeY, rotationZ);

            lastPlatform = instance;
            lastRightEdgeX = instance.GetRightEdgeWorld().x;
            lastEdgeY = instance.GetRightEdgeWorld().y;
            active.Enqueue(instance);

            if (forceSafe) spawnsSinceSafety = 0;
        }

        float ComputeSafeGap(float heightDelta)
        {
            if (heightDelta <= 0.05f)
            {
                float descendBonus = Mathf.Abs(Mathf.Min(0f, heightDelta)) * 0.5f;
                return envelope.maxForwardDistance + descendBonus;
            }

            float clampedDelta = Mathf.Min(heightDelta, envelope.maxUpwardHeight);
            float t = envelope.maxUpwardHeight <= 0f ? 0f : clampedDelta / envelope.maxUpwardHeight;
            return Mathf.Lerp(envelope.maxForwardDistance, envelope.maxForwardAtMaxHeight, t);
        }

        void PositionPlatform(PlatformBlock block, float leftEdgeX, float leftEdgeY, float rotationZ)
        {
            block.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, 0f, rotationZ));

            Vector3 currentLeft = block.GetLeftEdgeWorld();
            Vector3 delta = new Vector3(leftEdgeX - currentLeft.x, leftEdgeY - currentLeft.y, 0f);
            block.transform.position += delta;
        }

        void DespawnBehind()
        {
            float cutoff = player.position.x - despawnBehindDistance;
            while (active.Count > 0 && active.Peek().GetRightEdgeWorld().x < cutoff)
                ReturnToPool(active.Dequeue());
        }

        PlatformBlock ChoosePrefab()
        {
            float total = 0f;
            foreach (var p in platformPrefabs) total += p.spawnWeight;

            float roll = Random.Range(0f, total);
            float accum = 0f;
            foreach (var p in platformPrefabs)
            {
                accum += p.spawnWeight;
                if (roll <= accum) return p;
            }
            return platformPrefabs[platformPrefabs.Length - 1];
        }

        PlatformBlock GetFromPool(PlatformBlock prefab)
        {
            if (!pools.TryGetValue(prefab, out var pool))
            {
                pool = new Queue<PlatformBlock>();
                pools[prefab] = pool;
            }

            if (pool.Count > 0)
            {
                PlatformBlock reused = pool.Dequeue();
                reused.gameObject.SetActive(true);
                return reused;
            }

            PlatformBlock created = Instantiate(prefab, transform);
            instanceSource[created] = prefab;
            return created;
        }

        void ReturnToPool(PlatformBlock instance)
        {
            if (!instanceSource.TryGetValue(instance, out var prefab)) return;
            instance.gameObject.SetActive(false);
            pools[prefab].Enqueue(instance);
        }

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || player == null) return;

            float px = player.position.x;
            float py = player.position.y;
            float h = gizmoLineHalfHeight;

            // spawn-trigger edge, ahead of the player
            Gizmos.color = spawnAheadColor;
            Gizmos.DrawLine(new Vector3(px + spawnAheadDistance, py - h, 0f), new Vector3(px + spawnAheadDistance, py + h, 0f));

            // despawn cutoff, behind the player
            Gizmos.color = despawnBehindColor;
            Gizmos.DrawLine(new Vector3(px - despawnBehindDistance, py - h, 0f), new Vector3(px - despawnBehindDistance, py + h, 0f));

            // dynamic ceiling - platforms never spawn above this line, and it always tracks the player's current Y
            Gizmos.color = ceilingColor;
            Gizmos.DrawLine(new Vector3(px, py + upperBoundOffset, 0f), new Vector3(px + spawnAheadDistance, py + upperBoundOffset, 0f));

            if (lastPlatform == null) return;

            Vector3 edge = new Vector3(lastRightEdgeX, lastEdgeY, 0f);

            // last spawned edge, and the vertical step band the next platform is allowed to pick from
            Gizmos.color = lastEdgeColor;
            Gizmos.DrawSphere(edge, 0.12f);
            Color band = lastEdgeColor;
            band.a = 0.35f;
            Gizmos.color = band;
            Gizmos.DrawLine(edge + Vector3.down * maxVerticalStep, edge + Vector3.up * maxVerticalStep);

            // reachability envelope from the last edge (only meaningful once the launch simulation has run)
            if (Application.isPlaying)
            {
                Gizmos.color = reachEnvelopeColor;
                Vector3 flatReach = edge + new Vector3(envelope.maxForwardDistance, 0f, 0f);
                Vector3 peakReach = edge + new Vector3(envelope.maxForwardAtMaxHeight, envelope.maxUpwardHeight, 0f);
                Gizmos.DrawLine(edge, peakReach);
                Gizmos.DrawLine(peakReach, flatReach);
                Gizmos.DrawLine(edge, flatReach);
            }
        }
    }
}