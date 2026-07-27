using System.Collections.Generic;
using UnityEngine;
using Valley.Core;

namespace Valley.Level.Generation
{
    /// <summary>
    /// Procedurally spawns PlatformBlock instances ahead of the player across 5 parallel layers: the
    /// mid layer (the actual traversal path, using the full launch-reachability logic) plus up to 4
    /// side layers configured in <see cref="sideLayers"/> - typically 2 above and 2 below. Side layers
    /// re-center on the mid layer's current edge height every spawn, and trend sparser going up /
    /// denser-and-more-likely-to-attach going down via each PlatformLayer's gapMultiplier and
    /// stickChanceBonus. Despawning is distance-driven per layer, and all layers share the same pool.
    /// </summary>
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

        [Header("Vertical Band (Mid Layer)")]
        [Tooltip("Platforms never generate above (player's current Y + this offset). Re-evaluated on every spawn, so it tracks the player instead of trapping them under a fixed ceiling.")]
        public float upperBoundOffset = 6f;
        [Tooltip("Max |change in edge height| allowed between two consecutive mid-layer platforms. There is deliberately no matching lower clamp.")]
        public float maxVerticalStep = 3f;

        [Header("Gap Control (base values, scaled per layer)")]
        public float minGapX = 0.5f;
        [Tooltip("Authored ceiling on mid-layer gap size; still further clamped by reachability.")]
        public float maxGapX = 4f;
        [Range(0f, 1f)] public float gapChance = 0.65f;

        [Header("Anti-Runaway Safety (Mid Layer)")]
        [Tooltip("Hard cap on flush attaches in a row, so blocks that allow sticking can't chain into an endless floor.")]
        public int maxConsecutiveSticks = 3;
        [Tooltip("After this many near-max-difficulty gaps in a row, the next gap eases off.")]
        public int maxConsecutiveHardGaps = 2;
        [Tooltip("Every N spawns, force a flat, unrotated, easy gap as a guaranteed-reachable checkpoint.")]
        public int guaranteedSafetyInterval = 8;

        [Header("Side Layers (2 up, 2 down)")]
        [Tooltip("Vertical spacing used by 'Build Default Side Layers' to auto-populate the array below.")]
        public float layerSpacing = 4f;
        public PlatformLayer[] sideLayers = new PlatformLayer[0];

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

        [ContextMenu("Build Default Side Layers (2 Up / 2 Down)")]
        void BuildDefaultSideLayers()
        {
            sideLayers = new[]
            {
                new PlatformLayer { label = "Up 2",   verticalOffset =  2f * layerSpacing, verticalJitter = 1.5f, gapMultiplier = 2.2f,  stickChanceBonus = 0f,   maxConsecutiveSticks = 1,   clampToReachability = false, gizmoColor = new Color(1f, 0.4f, 0.1f) },
                new PlatformLayer { label = "Up 1",   verticalOffset =  1f * layerSpacing, verticalJitter = 1.2f, gapMultiplier = 1.5f,  stickChanceBonus = 0f,   maxConsecutiveSticks = 2,   clampToReachability = false, gizmoColor = new Color(1f, 0.8f, 0.2f) },
                new PlatformLayer { label = "Down 1", verticalOffset = -1f * layerSpacing, verticalJitter = 0.8f, gapMultiplier = 0.65f, stickChanceBonus = 0.3f, maxConsecutiveSticks = 6,   clampToReachability = false, gizmoColor = new Color(0.2f, 0.8f, 1f) },
                new PlatformLayer { label = "Down 2", verticalOffset = -2f * layerSpacing, verticalJitter = 0.5f, gapMultiplier = 0.35f, stickChanceBonus = 0.6f, maxConsecutiveSticks = 999, clampToReachability = false, gizmoColor = new Color(0.2f, 0.3f, 1f) },
            };
        }

        void Start()
        {
            envelope = LaunchReachability.Calculate(forwardSpeed, launchProfile, gravity, maxLaunches);
            SpawnInitial();

            foreach (var layer in sideLayers)
            {
                if (layer == null) continue;
                SpawnInitialSideLayer(layer);
            }
        }

        void Update()
        {
            if (player == null || platformPrefabs == null || platformPrefabs.Length == 0) return;

            while (player.position.x + spawnAheadDistance > lastRightEdgeX)
                SpawnNext();
            DespawnBehind();

            foreach (var layer in sideLayers)
            {
                if (layer == null) continue;

                while (player.position.x + spawnAheadDistance > layer.lastRightEdgeX)
                    SpawnNextSideLayer(layer);
                DespawnBehindLayer(layer);
            }
        }

        // ---------------------------------------------------------------
        // Mid layer (the traversal path) - unchanged reachability-aware logic
        // ---------------------------------------------------------------

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

            PlatformBlock prefab = ChoosePrefab(platformPrefabs);

            bool wantsGap = forceSafe || Random.value < gapChance;
            bool canStick = !wantsGap && lastPlatform != null
                             && lastPlatform.rightAttach.allowed && prefab.leftAttach.allowed
                             && consecutiveSticks < maxConsecutiveSticks;
            bool stick = canStick && Random.value <= Mathf.Min(lastPlatform.rightAttach.successRate, prefab.leftAttach.successRate);

            float targetLeftEdgeY;
            float gapX;

            if (stick)
            {
                targetLeftEdgeY = lastEdgeY;
                gapX = 0f;
                consecutiveSticks++;
                consecutiveHardGaps = 0;
            }
            else
            {
                consecutiveSticks = 0;

                float dynamicCeiling = player.position.y + upperBoundOffset;
                float minY = lastEdgeY - maxVerticalStep;
                float maxY = Mathf.Min(lastEdgeY + maxVerticalStep, dynamicCeiling);
                if (maxY < minY) maxY = minY;

                float rawTarget = forceSafe ? lastEdgeY : Random.Range(minY, maxY);

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

        void DespawnBehind()
        {
            float cutoff = player.position.x - despawnBehindDistance;
            while (active.Count > 0 && active.Peek().GetRightEdgeWorld().x < cutoff)
                ReturnToPool(active.Dequeue());
        }

        // ---------------------------------------------------------------
        // Side layers - simpler logic, re-centered on the mid layer each spawn
        // ---------------------------------------------------------------

        void SpawnInitialSideLayer(PlatformLayer layer)
        {
            PlatformBlock[] pool = layer.ResolvePrefabPool(platformPrefabs);
            PlatformBlock prefab = pool[0];
            PlatformBlock instance = GetFromPool(prefab);

            float startLeftX = player.position.x - prefab.Width * 0.5f;
            float startLeftY = lastEdgeY + layer.verticalOffset;
            PositionPlatform(instance, startLeftX, startLeftY, 0f);

            layer.lastPlatform = instance;
            layer.lastRightEdgeX = instance.GetRightEdgeWorld().x;
            layer.lastEdgeY = instance.GetRightEdgeWorld().y;
            layer.active.Enqueue(instance);
        }

        void SpawnNextSideLayer(PlatformLayer layer)
        {
            PlatformBlock[] pool = layer.ResolvePrefabPool(platformPrefabs);
            PlatformBlock prefab = ChoosePrefab(pool);

            bool canStick = layer.lastPlatform != null
                             && layer.lastPlatform.rightAttach.allowed && prefab.leftAttach.allowed
                             && layer.consecutiveSticks < layer.maxConsecutiveSticks;
            float stickRoll = canStick
                ? Mathf.Min(layer.lastPlatform.rightAttach.successRate, prefab.leftAttach.successRate) + layer.stickChanceBonus
                : 0f;
            bool stick = canStick && Random.value <= Mathf.Clamp01(stickRoll);

            float targetLeftEdgeY;
            float gapX;

            if (stick)
            {
                targetLeftEdgeY = layer.lastEdgeY;
                gapX = 0f;
                layer.consecutiveSticks++;
            }
            else
            {
                layer.consecutiveSticks = 0;

                float baselineY = lastEdgeY + layer.verticalOffset; // rides along the mid layer's current path height
                float rawTarget = Random.Range(baselineY - layer.verticalJitter, baselineY + layer.verticalJitter);

                float scaledMin = Mathf.Max(0.05f, minGapX * layer.gapMultiplier);
                float scaledMax;

                if (layer.clampToReachability)
                {
                    float maxReachableY = layer.lastEdgeY + Mathf.Max(0f, envelope.maxUpwardHeight - reachabilitySafetyMargin);
                    targetLeftEdgeY = Mathf.Min(rawTarget, maxReachableY);

                    float heightDelta = targetLeftEdgeY - layer.lastEdgeY;
                    float safeGap = Mathf.Max(minGapX, ComputeSafeGap(heightDelta) - reachabilitySafetyMargin);
                    scaledMax = Mathf.Max(scaledMin, Mathf.Min(maxGapX, safeGap) * layer.gapMultiplier);
                }
                else
                {
                    targetLeftEdgeY = rawTarget;
                    scaledMax = Mathf.Max(scaledMin, maxGapX * layer.gapMultiplier);
                }

                gapX = Random.Range(scaledMin, scaledMax);
            }

            float spawnLeftEdgeX = layer.lastRightEdgeX + gapX;
            float rotationZ = prefab.rotation.allowRotation
                ? Random.Range(prefab.rotation.minAngleDegrees, prefab.rotation.maxAngleDegrees)
                : 0f;

            PlatformBlock instance = GetFromPool(prefab);
            PositionPlatform(instance, spawnLeftEdgeX, targetLeftEdgeY, rotationZ);

            layer.lastPlatform = instance;
            layer.lastRightEdgeX = instance.GetRightEdgeWorld().x;
            layer.lastEdgeY = instance.GetRightEdgeWorld().y;
            layer.active.Enqueue(instance);
        }

        void DespawnBehindLayer(PlatformLayer layer)
        {
            float cutoff = player.position.x - despawnBehindDistance;
            while (layer.active.Count > 0 && layer.active.Peek().GetRightEdgeWorld().x < cutoff)
                ReturnToPool(layer.active.Dequeue());
        }

        // ---------------------------------------------------------------
        // Shared helpers
        // ---------------------------------------------------------------

        void PositionPlatform(PlatformBlock block, float leftEdgeX, float leftEdgeY, float rotationZ)
        {
            block.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, 0f, rotationZ));

            Vector3 currentLeft = block.GetLeftEdgeWorld();
            Vector3 delta = new Vector3(leftEdgeX - currentLeft.x, leftEdgeY - currentLeft.y, 0f);
            block.transform.position += delta;
        }

        PlatformBlock ChoosePrefab(PlatformBlock[] pool)
        {
            float total = 0f;
            foreach (var p in pool) total += p.spawnWeight;

            float roll = Random.Range(0f, total);
            float accum = 0f;
            foreach (var p in pool)
            {
                accum += p.spawnWeight;
                if (roll <= accum) return p;
            }
            return pool[pool.Length - 1];
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

        void OnDrawGizmos()
        {
            if (!showDebugGizmos || player == null) return;

            float px = player.position.x;
            float py = player.position.y;
            float h = gizmoLineHalfHeight;

            Gizmos.color = spawnAheadColor;
            Gizmos.DrawLine(new Vector3(px + spawnAheadDistance, py - h, 0f), new Vector3(px + spawnAheadDistance, py + h, 0f));

            Gizmos.color = despawnBehindColor;
            Gizmos.DrawLine(new Vector3(px - despawnBehindDistance, py - h, 0f), new Vector3(px - despawnBehindDistance, py + h, 0f));

            Gizmos.color = ceilingColor;
            Gizmos.DrawLine(new Vector3(px, py + upperBoundOffset, 0f), new Vector3(px + spawnAheadDistance, py + upperBoundOffset, 0f));

            if (lastPlatform != null)
            {
                Vector3 edge = new Vector3(lastRightEdgeX, lastEdgeY, 0f);

                Gizmos.color = lastEdgeColor;
                Gizmos.DrawSphere(edge, 0.12f);
                Color band = lastEdgeColor;
                band.a = 0.35f;
                Gizmos.color = band;
                Gizmos.DrawLine(edge + Vector3.down * maxVerticalStep, edge + Vector3.up * maxVerticalStep);

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

            foreach (var layer in sideLayers)
            {
                if (layer == null) continue;

                float baseline = lastEdgeY + layer.verticalOffset;
                Color faint = layer.gizmoColor;
                faint.a = 0.25f;
                Gizmos.color = faint;
                Gizmos.DrawLine(new Vector3(px, baseline, 0f), new Vector3(px + spawnAheadDistance, baseline, 0f));

                if (layer.lastPlatform == null) continue;

                Vector3 layerEdge = new Vector3(layer.lastRightEdgeX, layer.lastEdgeY, 0f);
                Gizmos.color = layer.gizmoColor;
                Gizmos.DrawSphere(layerEdge, 0.1f);

                Color layerBand = layer.gizmoColor;
                layerBand.a = 0.35f;
                Gizmos.color = layerBand;
                Gizmos.DrawLine(layerEdge + Vector3.down * layer.verticalJitter, layerEdge + Vector3.up * layer.verticalJitter);
            }
        }
    }
}