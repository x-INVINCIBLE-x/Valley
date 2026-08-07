using UnityEngine;
using Valley.Core;
using Valley.Core.Pooling;

namespace Valley.Level.Generation
{
    /// <summary>
    /// Procedurally spawns PlatformBlock instances across 5 parallel layers: the mid layer (the actual
    /// traversal path, using the full launch-reachability logic) plus up to 4 side layers configured in
    /// <see cref="sideLayers"/>.
    ///
    /// Every layer keeps a permanent, append-only history of every record it has ever generated
    /// (prefab + position + rotation), separate from which of those records currently have a live
    /// GameObject. Moving forward grows the live window and, once the history runs out, generates and
    /// appends new records; moving backward re-materializes existing records from history instead of
    /// rolling new RNG, so revisiting an area reproduces the exact same layout. GameObjects themselves
    /// are recycled through a generic PrefabPoolGroup rather than being destroyed.
    ///
    /// The whole generation state (history, live instances, world-shift offset, streak counters) is
    /// reset every time the component is enabled or disabled: OnDisable releases every live platform
    /// back to the pool, and OnEnable wipes both runtimes' history and reseeds from scratch relative to
    /// the player's current position - equivalent to a fresh Start(). The object pool itself persists
    /// across enable/disable so recycled instances keep getting reused instead of being rebuilt.
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
        [Tooltip("Distance in X from the player at which the very first platform of every layer is seeded when this component is enabled (e.g. 100 seeds the first platform 100 units ahead of the player). 0 seeds it centered on the player, matching the old behavior. Keep this at or below spawnAheadDistance - if it's larger, the seeded platform starts outside the spawn-ahead window and will be immediately despawned again until the player gets close enough.")]
        public float startingOffsetX = 0f;

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

        [Header("Score-Based Spawning")]
        [Tooltip("How many times a candidate's spawnChance is allowed to fail in a row before the last-picked candidate is placed anyway, guaranteeing forward progress.")]
        public int maxSpawnChanceAttempts = 4;

        [Header("History Retention")]
        [Tooltip("How many already-despawned platforms behind the live window each layer keeps remembered, so backtracking reproduces the same layout. Beyond this, the oldest records are discarded and that ground regenerates fresh if revisited. 0 = unlimited (never trimmed).")]
        public int historyRetentionCount = 200;

        [Header("Anti-Runaway Safety (Mid Layer)")]
        [Tooltip("Hard cap on flush attaches in a row, so blocks that allow sticking can't chain into an endless floor.")]
        public int maxConsecutiveSticks = 3;
        [Tooltip("After this many near-max-difficulty gaps in a row, the next gap eases off.")]
        public int maxConsecutiveHardGaps = 2;
        [Tooltip("Every N newly-generated platforms, force a flat, unrotated, easy gap as a guaranteed-reachable checkpoint.")]
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
        PrefabPoolGroup<PlatformBlock> objectPool;
        readonly PlatformLayerRuntime midRuntime = new PlatformLayerRuntime();

        float worldShiftOffset;

        /// <summary>
        /// player.position.x corrected for every WorldShiftEvents.OnWorldShiftedX broadcast so far. If
        /// progress is made by moving the player, this is just player.position.x. If progress is instead
        /// made by shifting the world/platforms backward under a stationary-ish player, player.position.x
        /// alone never advances - this is what keeps the spawn/despawn window actually moving either way.
        /// </summary>
        float PlayerProgressX => player.position.x + worldShiftOffset;

        void Awake()
        {
            // Pool survives enable/disable cycles - recycled instances just get reused across resets
            // instead of being torn down and rebuilt every time.
            objectPool = new PrefabPoolGroup<PlatformBlock>(transform);
        }

        void OnEnable()
        {
            WorldShiftEvents.OnWorldShiftedX += HandleWorldShift;

            // Guard against edit-mode enable (e.g. toggling the component checkbox without pressing
            // Play), where Awake never ran and objectPool would still be null.
            if (!Application.isPlaying) return;

            ResetAndSeed();
        }

        void OnDisable()
        {
            WorldShiftEvents.OnWorldShiftedX -= HandleWorldShift;

            if (!Application.isPlaying) return;

            DeleteAllPlatforms();
        }

        void HandleWorldShift(float amountSubtractedFromWorld) => worldShiftOffset += amountSubtractedFromWorld;

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

        void Update()
        {
            if (player == null || platformPrefabs == null || platformPrefabs.Length == 0) return;

            AdvanceWindow(midRuntime, true, null);
            foreach (var layer in sideLayers)
            {
                if (layer == null) continue;
                AdvanceWindow(layer.runtime, false, layer);
            }
        }

        // ---------------------------------------------------------------
        // Reset + seeding (runs fresh every OnEnable)
        // ---------------------------------------------------------------

        /// <summary>
        /// Wipes every layer's history and live instances, resets the world-shift offset and streak
        /// counters, recalculates the reachability envelope (in case reachability-related fields were
        /// tweaked while disabled), and reseeds every layer relative to the player's current position.
        /// Public so a level-restart / respawn system can force this without needing to toggle the
        /// component's enabled state.
        /// </summary>
        public void ResetAndSeed()
        {
            worldShiftOffset = 0f;
            envelope = LaunchReachability.Calculate(forwardSpeed, launchProfile, gravity, maxLaunches);

            midRuntime.Reset();
            foreach (var layer in sideLayers)
            {
                if (layer == null) continue;
                layer.runtime.Reset();
            }

            if (player == null || platformPrefabs == null || platformPrefabs.Length == 0) return;

            SeedMid();
            foreach (var layer in sideLayers)
            {
                if (layer == null) continue;
                SeedSideLayer(layer);
            }
        }

        void SeedMid()
        {
            PlatformBlock prefab = platformPrefabs[0];
            float startLeftX = PlayerProgressX + startingOffsetX - prefab.Width * 0.5f;
            float startLeftY = player.position.y - 0.1f;
            midRuntime.AddRecord(new PlatformRecord { prefab = prefab, leftEdgeX = startLeftX, leftEdgeY = startLeftY, rotationZ = 0f });
            MaterializeAppend(midRuntime, midRuntime.LastGlobalIndex);
        }

        void SeedSideLayer(PlatformLayer layer)
        {
            PlatformBlock prefab = layer.ResolvePrefabPool(platformPrefabs)[0];
            float startLeftX = PlayerProgressX + startingOffsetX - prefab.Width * 0.5f;
            float startLeftY = midRuntime.GetRecord(midRuntime.LastGlobalIndex).rightEdgeY + layer.verticalOffset;
            layer.runtime.AddRecord(new PlatformRecord { prefab = prefab, leftEdgeX = startLeftX, leftEdgeY = startLeftY, rotationZ = 0f });
            MaterializeAppend(layer.runtime, layer.runtime.LastGlobalIndex);
        }

        // ---------------------------------------------------------------
        // Bidirectional window management (shared by every layer)
        // ---------------------------------------------------------------

        void AdvanceWindow(PlatformLayerRuntime r, bool isMid, PlatformLayer layer)
        {
            float aheadBound = PlayerProgressX + spawnAheadDistance;
            float behindBound = PlayerProgressX - despawnBehindDistance;

            // Grow right: materialize records that already exist in history, generating new ones only
            // once the recorded frontier itself falls short of the ahead boundary.
            while (true)
            {
                int nextIndex = r.liveInstances.Count == 0 ? r.liveStartIndex : r.liveStartIndex + r.liveInstances.Count;

                if (nextIndex > r.LastGlobalIndex)
                {
                    float frontierRight = r.GetRecord(r.LastGlobalIndex).rightEdgeX;
                    if (frontierRight >= aheadBound) break;
                    if (isMid) GenerateMidRecord(r); else GenerateSideRecord(r, layer);
                }

                if (r.GetRecord(nextIndex).leftEdgeX >= aheadBound) break;
                MaterializeAppend(r, nextIndex);
            }

            // Grow left: the player has moved back into an area that was despawned but is still recorded.
            // If it's already been trimmed past historyRetentionCount, there's nothing left to bring back.
            while (r.liveStartIndex > r.historyBaseIndex && r.GetRecord(r.liveStartIndex - 1).rightEdgeX > behindBound)
            {
                MaterializePrepend(r, r.liveStartIndex - 1);
                r.liveStartIndex--;
            }

            // Shrink left: despawn what's fallen behind the player.
            while (r.liveInstances.Count > 0 && r.GetRecord(r.liveStartIndex).rightEdgeX < behindBound)
            {
                ReleaseFront(r);
                r.liveStartIndex++;
            }

            // Shrink right: despawn what's now too far ahead (player moved back a long way).
            while (r.liveInstances.Count > 0 && r.GetRecord(r.liveStartIndex + r.liveInstances.Count - 1).leftEdgeX > aheadBound)
            {
                ReleaseBack(r);
            }

            TrimHistory(r);
        }

        /// <summary>Permanently forgets already-despawned records once more than historyRetentionCount of them have piled up behind the live window.</summary>
        void TrimHistory(PlatformLayerRuntime r)
        {
            if (historyRetentionCount <= 0) return;

            int despawnedBehindCount = r.liveStartIndex - r.historyBaseIndex;
            int excess = despawnedBehindCount - historyRetentionCount;
            if (excess <= 0) return;

            r.TrimFront(excess);
        }

        void MaterializeAppend(PlatformLayerRuntime r, int index)
        {
            PlatformRecord record = r.GetRecord(index);
            PlatformBlock instance = objectPool.Get(record.prefab);
            PositionPlatform(instance, record.leftEdgeX, record.leftEdgeY, record.rotationZ);

            Vector3 rightEdge = instance.GetRightEdgeWorld();
            record.rightEdgeX = rightEdge.x;
            record.rightEdgeY = rightEdge.y;
            r.SetRecord(index, record);

            r.liveInstances.Add(instance);
            if (r.liveInstances.Count == 1) r.liveStartIndex = index;
        }

        void MaterializePrepend(PlatformLayerRuntime r, int index)
        {
            PlatformRecord record = r.GetRecord(index);
            PlatformBlock instance = objectPool.Get(record.prefab);
            PositionPlatform(instance, record.leftEdgeX, record.leftEdgeY, record.rotationZ);

            Vector3 rightEdge = instance.GetRightEdgeWorld();
            record.rightEdgeX = rightEdge.x;
            record.rightEdgeY = rightEdge.y;
            r.SetRecord(index, record);

            r.liveInstances.Insert(0, instance);
        }

        void ReleaseFront(PlatformLayerRuntime r)
        {
            objectPool.Release(r.liveInstances[0]);
            r.liveInstances.RemoveAt(0);
        }

        void ReleaseBack(PlatformLayerRuntime r)
        {
            int last = r.liveInstances.Count - 1;
            objectPool.Release(r.liveInstances[last]);
            r.liveInstances.RemoveAt(last);
        }

        public void DeleteAllPlatforms()
        {
            ReleaseAll(midRuntime);

            foreach (var layer in sideLayers)
            {
                if (layer == null)
                    continue;

                ReleaseAll(layer.runtime);
            }
        }

        private void ReleaseAll(PlatformLayerRuntime runtime)
        {
            while (runtime.liveInstances.Count > 0)
            {
                objectPool.Release(runtime.liveInstances[^1]);
                runtime.liveInstances.RemoveAt(runtime.liveInstances.Count - 1);
            }
        }

        // ---------------------------------------------------------------
        // Record generation (only ever called when extending the frontier)
        // ---------------------------------------------------------------

        void GenerateMidRecord(PlatformLayerRuntime r)
        {
            r.spawnsSinceSafety++;
            bool forceSafe = r.spawnsSinceSafety >= guaranteedSafetyInterval;

            PlatformRecord prev = r.GetRecord(r.LastGlobalIndex);
            PlatformBlock prefab = ChooseSpawnablePrefab(platformPrefabs, out float missedGap);

            bool wantsGap = forceSafe || Random.value < gapChance;
            bool canStick = !wantsGap && prev.prefab.rightAttach.allowed && prefab.leftAttach.allowed
                             && r.consecutiveSticks < maxConsecutiveSticks;
            bool stick = canStick && Random.value <= Mathf.Min(prev.prefab.rightAttach.successRate, prefab.leftAttach.successRate);

            float targetLeftEdgeY;
            float gapX;

            if (stick)
            {
                targetLeftEdgeY = prev.rightEdgeY;
                gapX = 0f;
                r.consecutiveSticks++;
                r.consecutiveHardGaps = 0;
            }
            else
            {
                r.consecutiveSticks = 0;

                float dynamicCeiling = player.position.y + upperBoundOffset;
                float minY = prev.rightEdgeY - maxVerticalStep;
                float maxY = Mathf.Min(prev.rightEdgeY + maxVerticalStep, dynamicCeiling);
                if (maxY < minY) maxY = minY;

                float rawTarget = forceSafe ? prev.rightEdgeY : Random.Range(minY, maxY);

                float maxReachableY = prev.rightEdgeY + Mathf.Max(0f, envelope.maxUpwardHeight - reachabilitySafetyMargin);
                targetLeftEdgeY = Mathf.Min(rawTarget, maxReachableY);

                float heightDelta = targetLeftEdgeY - prev.rightEdgeY;
                float safeGap = Mathf.Max(minGapX, ComputeSafeGap(heightDelta) - reachabilitySafetyMargin);
                float hardCap = forceSafe ? Mathf.Min(minGapX * 1.5f, safeGap) : Mathf.Min(maxGapX, safeGap);

                bool isHardGap = !forceSafe && hardCap >= safeGap * 0.85f;
                r.consecutiveHardGaps = isHardGap ? r.consecutiveHardGaps + 1 : 0;
                if (r.consecutiveHardGaps > maxConsecutiveHardGaps) hardCap *= 0.6f;

                gapX = Random.Range(minGapX, Mathf.Max(minGapX, hardCap));
            }

            float spawnLeftEdgeX = prev.rightEdgeX + gapX + missedGap;
            float rotationZ = (!forceSafe && prefab.rotation.allowRotation)
                ? Random.Range(prefab.rotation.minAngleDegrees, prefab.rotation.maxAngleDegrees)
                : 0f;

            r.AddRecord(new PlatformRecord { prefab = prefab, leftEdgeX = spawnLeftEdgeX, leftEdgeY = targetLeftEdgeY, rotationZ = rotationZ });

            if (forceSafe) r.spawnsSinceSafety = 0;
        }

        void GenerateSideRecord(PlatformLayerRuntime r, PlatformLayer layer)
        {
            PlatformRecord prev = r.GetRecord(r.LastGlobalIndex);
            PlatformBlock[] pool = layer.ResolvePrefabPool(platformPrefabs);
            PlatformBlock prefab = ChooseSpawnablePrefab(pool, out float missedGap);

            bool canStick = prev.prefab.rightAttach.allowed && prefab.leftAttach.allowed
                             && r.consecutiveSticks < layer.maxConsecutiveSticks;
            float stickRoll = canStick
                ? Mathf.Min(prev.prefab.rightAttach.successRate, prefab.leftAttach.successRate) + layer.stickChanceBonus
                : 0f;
            bool stick = canStick && Random.value <= Mathf.Clamp01(stickRoll);

            float targetLeftEdgeY;
            float gapX;

            if (stick)
            {
                targetLeftEdgeY = prev.rightEdgeY;
                gapX = 0f;
                r.consecutiveSticks++;
            }
            else
            {
                r.consecutiveSticks = 0;

                float baselineY = midRuntime.GetRecord(midRuntime.LastGlobalIndex).rightEdgeY + layer.verticalOffset;
                float rawTarget = Random.Range(baselineY - layer.verticalJitter, baselineY + layer.verticalJitter);

                float scaledMin = Mathf.Max(0.05f, minGapX * layer.gapMultiplier);
                float scaledMax;

                if (layer.clampToReachability)
                {
                    float maxReachableY = prev.rightEdgeY + Mathf.Max(0f, envelope.maxUpwardHeight - reachabilitySafetyMargin);
                    targetLeftEdgeY = Mathf.Min(rawTarget, maxReachableY);

                    float heightDelta = targetLeftEdgeY - prev.rightEdgeY;
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

            float spawnLeftEdgeX = prev.rightEdgeX + gapX + missedGap;
            float rotationZ = prefab.rotation.allowRotation
                ? Random.Range(prefab.rotation.minAngleDegrees, prefab.rotation.maxAngleDegrees)
                : 0f;

            r.AddRecord(new PlatformRecord { prefab = prefab, leftEdgeX = spawnLeftEdgeX, leftEdgeY = targetLeftEdgeY, rotationZ = rotationZ });
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

        PlatformBlock ChoosePrefab(PlatformBlock[] prefabPool)
        {
            float total = 0f;
            foreach (var p in prefabPool) total += p.spawnWeight;

            float roll = Random.Range(0f, total);
            float accum = 0f;
            foreach (var p in prefabPool)
            {
                accum += p.spawnWeight;
                if (roll <= accum) return p;
            }
            return prefabPool[prefabPool.Length - 1];
        }

        /// <summary>
        /// Weighted-picks a candidate, then rolls its spawnChance. On failure it re-picks and adds that
        /// candidate's width plus a base gap to extraGap - the space it would have occupied becomes part
        /// of the gap before whatever finally gets placed. After maxSpawnChanceAttempts failures in a
        /// row, the last candidate is returned regardless, so a generation step always produces exactly
        /// one record and forward progress is never blocked by an unlucky run of chance rolls.
        /// </summary>
        PlatformBlock ChooseSpawnablePrefab(PlatformBlock[] prefabPool, out float extraGap)
        {
            extraGap = 0f;
            PlatformBlock prefab = ChoosePrefab(prefabPool);

            for (int attempt = 0; attempt < maxSpawnChanceAttempts; attempt++)
            {
                if (Random.value <= prefab.spawnChance) return prefab;

                extraGap += prefab.Width + minGapX;
                prefab = ChoosePrefab(prefabPool);
            }

            return prefab;
        }

        void OnDrawGizmos()
        {
            if (!showDebugGizmos || player == null) return;

            float px = PlayerProgressX;
            float py = player.position.y;
            float h = gizmoLineHalfHeight;

            Gizmos.color = spawnAheadColor;
            Gizmos.DrawLine(new Vector3(px + spawnAheadDistance, py - h, 0f), new Vector3(px + spawnAheadDistance, py + h, 0f));

            Gizmos.color = despawnBehindColor;
            Gizmos.DrawLine(new Vector3(px - despawnBehindDistance, py - h, 0f), new Vector3(px - despawnBehindDistance, py + h, 0f));

            Gizmos.color = ceilingColor;
            Gizmos.DrawLine(new Vector3(px, py + upperBoundOffset, 0f), new Vector3(px + spawnAheadDistance, py + upperBoundOffset, 0f));

            DrawLayerGizmo(midRuntime, lastEdgeColor, maxVerticalStep);

            if (Application.isPlaying && midRuntime.RecordCount > 0)
            {
                PlatformRecord frontier = midRuntime.GetRecord(midRuntime.LastGlobalIndex);
                Vector3 edge = new Vector3(frontier.rightEdgeX, frontier.rightEdgeY, 0f);
                Gizmos.color = reachEnvelopeColor;
                Vector3 flatReach = edge + new Vector3(envelope.maxForwardDistance, 0f, 0f);
                Vector3 peakReach = edge + new Vector3(envelope.maxForwardAtMaxHeight, envelope.maxUpwardHeight, 0f);
                Gizmos.DrawLine(edge, peakReach);
                Gizmos.DrawLine(peakReach, flatReach);
                Gizmos.DrawLine(edge, flatReach);
            }

            foreach (var layer in sideLayers)
            {
                if (layer == null) continue;

                if (midRuntime.RecordCount > 0)
                {
                    float baseline = midRuntime.GetRecord(midRuntime.LastGlobalIndex).rightEdgeY + layer.verticalOffset;
                    Color faint = layer.gizmoColor;
                    faint.a = 0.25f;
                    Gizmos.color = faint;
                    Gizmos.DrawLine(new Vector3(px, baseline, 0f), new Vector3(px + spawnAheadDistance, baseline, 0f));
                }

                DrawLayerGizmo(layer.runtime, layer.gizmoColor, layer.verticalJitter);
            }
        }

        void DrawLayerGizmo(PlatformLayerRuntime r, Color color, float bandHalfHeight)
        {
            if (r.RecordCount == 0) return;

            PlatformRecord frontier = r.GetRecord(r.LastGlobalIndex);
            Vector3 edge = new Vector3(frontier.rightEdgeX, frontier.rightEdgeY, 0f);

            Gizmos.color = color;
            Gizmos.DrawSphere(edge, 0.1f);
            Color band = color;
            band.a = 0.35f;
            Gizmos.color = band;
            Gizmos.DrawLine(edge + Vector3.down * bandHalfHeight, edge + Vector3.up * bandHalfHeight);
        }
    }
}