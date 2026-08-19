using System.Collections.Generic;
using UnityEngine;
using Valley.Core;
using Valley.Core.Pooling;
using Valley.Scoring;

namespace Valley.Level.Generation
{
    /// <summary>
    /// Procedurally spawns PlatformBlock instances across 5 parallel layers: the mid layer (the actual
    /// traversal path, using the full launch-reachability logic) plus up to 4 side layers configured in
    /// <see cref="sideLayers"/>. Each layer resolves its own prefab spawn weights independently - the mid
    /// layer via GetPrefabWeight (profile weights + runtime overrides), side layers via their own
    /// PlatformLayer.prefabWeights + PlatformLayer overrides - so the same prefab can be common in one
    /// layer and rare (or absent) in another.

    public class PlatformChunkSpawner : MonoBehaviour
    {
        /// <summary>
        /// One entry in <see cref="progressionStages"/>. Triggers exactly once, the first time
        /// <see cref="CurrentDistance"/> reaches <see cref="distanceThreshold"/>. A stage can carry a new
        /// profile, a premade level, both, or neither - whatever's left null is simply skipped, so
        /// "continue with the next profile if there is one, otherwise keep the current one" and "insert a
        /// premade level" are independent knobs rather than an either/or choice.
        /// </summary>
        [System.Serializable]
        public class PlatformProgressionStage
        {
            [Tooltip("Stage triggers the first time CurrentDistance >= this value.")]
            public float distanceThreshold = 500f;

            [Tooltip("Optional. If assigned, every generation field this profile covers overwrites the spawner's current values once this stage triggers - i.e. 'move on to the next data set'. Leave empty to keep whatever profile is already active.")]
            public PlatformGenerationProfile profile;

            [Tooltip("Optional. If assigned, this single PlatformBlock is inserted into the MID layer as the very next platform once this stage triggers, positioned flush after whichever platform precedes it (leftEdgeX = prev.rightEdgeX + premadeLevelGap, leftEdgeY = prev.rightEdgeY) - exactly like any other generated block, just picked deterministically instead of rolled. Give it a single PlatformBlock component whose bounds/anchors span the whole hand-built segment, so normal generation can pick back up cleanly from its right edge afterward.")]
            public PlatformBlock premadeLevel;
        }

        [Header("References")]
        public Transform player;
        public PlatformBlock[] platformPrefabs;

        [Header("Prefab Weighting")]
        [Tooltip("Fallback weight shared by every layer, used for any prefab that has no entry in that layer's own weight table. Mid layer: the active profile's prefabWeights, then a runtime override via SetPrefabWeight(prefab, weight). Side layers: that PlatformLayer's own prefabWeights array, then a runtime override via SetPrefabWeight(layer, prefab, weight). Each layer's weighting is fully independent - the same prefab can be common in one layer and rare in another.")]
        public float defaultPrefabWeight = 1f;

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

        [Header("Depth Variance (Z Noise)")]
        [Tooltip("Random Z offset added on top of this spawner's own Z position for every platform spawned, in mid or side layers alike. Leave both at 0 to keep the old fixed-Z behavior. Rolled once per platform when its record is generated and stored in history, so revisiting an area keeps the same depth offset instead of re-rolling it. If min is set higher than max, the two are swapped automatically.")]
        public float zNoiseMin = 0f;
        public float zNoiseMax = 0f;

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

        [Header("Distance-Based Progression")]
        [Tooltip("Optional. If assigned, applied once during the very first ResetAndSeed (before the first platform is seeded), overwriting the fields above with this profile's values. Leave empty to just use the values set directly on this component, exactly like before this feature existed.")]
        public PlatformGenerationProfile initialProfile;
        [Tooltip("Sorted ascending by distanceThreshold automatically. Each entry triggers exactly once, the first time CurrentDistance reaches its threshold - see PlatformProgressionStage.")]
        public PlatformProgressionStage[] progressionStages = new PlatformProgressionStage[0];
        [Tooltip("Flush gap placed between the previous platform and a stage's premade level when one is inserted.")]
        public float premadeLevelGap = 0f;
        [Tooltip("Optional. If assigned, CurrentDistance reads from this tracker's Distance instead of being computed internally from player progress - use this to keep chunk-progression thresholds in lockstep with an on-screen distance/score readout driven by the same tracker.")]
        public DistanceScoreTracker distanceSource;

#if UNITY_EDITOR
        [Header("Testing (Editor Only)")]
        [Tooltip("Assign a profile here during Play Mode to apply it immediately - and to KEEP it applied live: editing any field on this profile asset's own Inspector re-applies it to this spawner automatically, so you can tune values while watching generation react in real time. This field and everything it does is compiled out of player builds. The Platform Chunk Spawner Tester window (Window > Valley > Platform Chunk Spawner Tester) does the same thing without needing this field, plus lets you test premade levels and weight overrides.")]
        public PlatformGenerationProfile testProfile;

        PlatformGenerationProfile subscribedTestProfile;
#endif

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
        float originAtReset;

        float distanceOriginX;
        int nextProgressionStageIndex;
        PlatformBlock pendingPremadeBlock;

        /// <summary>
        /// Right edge (logical X) of the most recently materialized premade level. Side-layer generation
        /// is clamped to never start before this, since side layers have no independent awareness of
        /// premade levels inserted into the mid layer - without this, a side layer's own gap/stick roll
        /// can place its next platform inside the premade segment's footprint. Reset to negative infinity
        /// (no clearance in effect) on ResetAndSeed and whenever the pending premade record is consumed.
        /// </summary>
        float sideLayerClearanceRightX = float.NegativeInfinity;

        /// <summary>
        /// Global mid-layer index of a just-queued premade record that's been added to history but not
        /// yet materialized/measured. MaterializeAppend watches for this index and, once it fires,
        /// promotes the record's freshly-measured rightEdgeX into <see cref="sideLayerClearanceRightX"/>.
        /// -1 when no premade record is pending materialization.
        /// </summary>
        int pendingPremadeRecordIndex = -1;

        /// <summary>Mid-layer base weights supplied by the currently active profile - fully replaced every ApplyProfile call. Side layers never read this; each has its own PlatformLayer.prefabWeights instead.</summary>
        readonly Dictionary<PlatformBlock, float> profileWeights = new Dictionary<PlatformBlock, float>();
        /// <summary>Mid-layer runtime overrides set via SetPrefabWeight(prefab, weight) - take priority over profileWeights and persist across profile swaps until explicitly cleared. For a side layer's runtime overrides, see SetPrefabWeight(layer, prefab, weight).</summary>
        readonly Dictionary<PlatformBlock, float> weightOverrides = new Dictionary<PlatformBlock, float>();

        /// <summary>
        /// Total accumulated shift between logical record space and current real Unity world-space X.
        /// Combines two independent ways "the world moves" can happen:
        ///  1) External systems explicitly broadcasting WorldShiftEvents.OnWorldShiftedX, accumulated into worldShiftOffset.
        ///  2) This component's OWN Transform being translated directly (e.g. a "world root" GameObject
        ///     that some scroller script moves every frame instead of firing the event). Since every
        ///     pooled platform is parented under this transform, live platforms are dragged along for
        ///     free by Unity's hierarchy - but PlayerProgressX needs to know about it too, or the
        ///     spawn/despawn window silently freezes whenever the player itself isn't also moving.
        /// </summary>
        float TotalShiftX => worldShiftOffset + (originAtReset - transform.position.x);

        /// <summary>
        /// player.position.x corrected for TotalShiftX. If progress is made by moving the player, this is
        /// just player.position.x. If progress is instead made by shifting the world/platforms backward
        /// under a stationary-ish player - whether via WorldShiftEvents or by moving this transform
        /// directly - player.position.x alone never advances; this is what keeps the spawn/despawn window
        /// actually moving either way.
        ///
        /// This is LOGICAL space (matches PlatformRecord.leftEdgeX / rightEdgeX), not necessarily real
        /// Unity world-space X - see RecordToWorldX / WorldToRecordX.
        /// </summary>
        float PlayerProgressX => player.position.x + TotalShiftX;

        /// <summary>Converts a record's logical X into the real Unity world-space X it should be placed/drawn at right now.</summary>
        float RecordToWorldX(float recordX) => recordX - TotalShiftX;

        /// <summary>Converts a real Unity world-space X (e.g. read back off a freshly positioned Transform) into logical record space.</summary>
        float WorldToRecordX(float worldX) => worldX + TotalShiftX;

        /// <summary>
        /// Distance covered since the last ResetAndSeed, driving <see cref="progressionStages"/>. Reads
        /// from <see cref="distanceSource"/> when one is assigned; otherwise computed internally from
        /// PlayerProgressX, which is a superset of what DistanceScoreTracker tracks (it also accounts for
        /// this spawner's own Transform being moved directly - see TotalShiftX).
        /// </summary>
        public float CurrentDistance => distanceSource != null ? distanceSource.Distance : (PlayerProgressX - distanceOriginX);

        /// <summary>Index into progressionStages of the next stage that hasn't triggered yet (equals progressionStages.Length once every stage has fired). Read-only - exposed for debug/tester UI.</summary>
        public int NextProgressionStageIndex => nextProgressionStageIndex;

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

#if UNITY_EDITOR
            if (subscribedTestProfile != null) subscribedTestProfile.Changed -= ReapplyTestProfile;
            subscribedTestProfile = null;
#endif

            if (!Application.isPlaying) return;

            DeleteAllPlatforms();
        }

        void HandleWorldShift(float amountSubtractedFromWorld) => worldShiftOffset += amountSubtractedFromWorld;

#if UNITY_EDITOR
        /// <summary>
        /// Unity calls this whenever a field changes in this component's own Inspector (including when
        /// testProfile is (re)assigned). Actually applying the profile is deferred via delayCall since
        /// OnValidate runs during Unity's serialization pass, and ApplyProfile itself writes serialized
        /// fields (platformPrefabs, forwardSpeed, etc.) - doing that synchronously from inside OnValidate
        /// is unsafe and logs warnings.
        /// </summary>
        void OnValidate()
        {
            if (!Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall += HandleTestProfileChanged;
        }

        void HandleTestProfileChanged()
        {
            if (this == null) return; // spawner may have been disabled/destroyed before delayCall fired

            if (subscribedTestProfile != testProfile)
            {
                if (subscribedTestProfile != null) subscribedTestProfile.Changed -= ReapplyTestProfile;
                subscribedTestProfile = testProfile;
                if (subscribedTestProfile != null) subscribedTestProfile.Changed += ReapplyTestProfile;
            }

            if (testProfile != null) ApplyProfile(testProfile);
        }

        void ReapplyTestProfile()
        {
            if (testProfile != null) ApplyProfile(testProfile);
        }
#endif

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

            CheckProgressionStages();

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
        /// tweaked while disabled), applies initialProfile if one is assigned, and reseeds every layer
        /// relative to the player's current position. Also resets distance-progression state, so a
        /// disable/enable cycle (or a manual call) restarts the profile progression from the beginning.
        /// Public so a level-restart / respawn system can force this without needing to toggle the
        /// component's enabled state.
        /// </summary>
        public void ResetAndSeed()
        {
            worldShiftOffset = 0f;
            originAtReset = transform.position.x;
            envelope = LaunchReachability.Calculate(forwardSpeed, launchProfile, gravity, maxLaunches);

            nextProgressionStageIndex = 0;
            pendingPremadeBlock = null;
            pendingPremadeRecordIndex = -1;
            sideLayerClearanceRightX = float.NegativeInfinity;
            SortProgressionStages();

            if (initialProfile != null) ApplyProfile(initialProfile, seedNewSideLayers: false);

            midRuntime.Reset();
            foreach (var layer in sideLayers)
            {
                if (layer == null) continue;
                layer.runtime.Reset();
            }

            if (player == null || platformPrefabs == null || platformPrefabs.Length == 0) return;

            distanceOriginX = PlayerProgressX;

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
            midRuntime.AddRecord(new PlatformRecord { prefab = prefab, leftEdgeX = startLeftX, leftEdgeY = startLeftY, rotationZ = 0f, zOffset = RollZNoise() });
            MaterializeAppend(midRuntime, midRuntime.LastGlobalIndex);
        }

        void SeedSideLayer(PlatformLayer layer)
        {
            PlatformBlock prefab = layer.ResolvePrefabPool(platformPrefabs)[0];
            float startLeftX = PlayerProgressX + startingOffsetX - prefab.Width * 0.5f;
            float startLeftY = midRuntime.GetRecord(midRuntime.LastGlobalIndex).rightEdgeY + layer.verticalOffset;
            layer.runtime.AddRecord(new PlatformRecord { prefab = prefab, leftEdgeX = startLeftX, leftEdgeY = startLeftY, rotationZ = 0f, zOffset = RollZNoise() });
            MaterializeAppend(layer.runtime, layer.runtime.LastGlobalIndex);
        }

        // ---------------------------------------------------------------
        // Distance-based progression
        // ---------------------------------------------------------------

        /// <summary>
        /// Checks CurrentDistance against progressionStages and fires every stage whose threshold has
        /// been reached since the last check, in order. Looping (rather than checking just the next one)
        /// means a single big jump in distance still fires every stage it skipped past, so
        /// nextProgressionStageIndex and "which profile is active" never fall out of sync.
        /// </summary>
        void CheckProgressionStages()
        {
            if (progressionStages == null) return;

            float currentDistance = CurrentDistance;
            while (nextProgressionStageIndex < progressionStages.Length
                   && progressionStages[nextProgressionStageIndex] != null
                   && currentDistance >= progressionStages[nextProgressionStageIndex].distanceThreshold)
            {
                PlatformProgressionStage stage = progressionStages[nextProgressionStageIndex];
                nextProgressionStageIndex++;

                // An empty stage.profile simply leaves whatever profile is already active in place -
                // this is what makes "continue with the next data set if there is one, otherwise keep
                // the current one" fall out naturally instead of needing special-case handling here.
                if (stage.profile != null) ApplyProfile(stage.profile);

                if (stage.premadeLevel != null) pendingPremadeBlock = stage.premadeLevel;
            }
        }

        /// <summary>
        /// Overwrites this spawner's generation fields with profile's values and immediately recalculates
        /// the reachability envelope (since forwardSpeed/launchProfile/gravity/maxLaunches may have just
        /// changed). Only affects generation from this point forward - already-materialized platforms and
        /// history are untouched, keeping backtracking reproducible. Public so profiles can also be
        /// swapped manually (e.g. from an editor tool or a non-distance gameplay trigger) instead of only
        /// through progressionStages.
        /// </summary>
        public void ApplyProfile(PlatformGenerationProfile profile) => ApplyProfile(profile, seedNewSideLayers: true);

        /// <summary>
        /// seedNewSideLayers is false only when called from ResetAndSeed's initialProfile step: at that
        /// point midRuntime hasn't been seeded yet (SeedSideLayer needs a mid-layer record to read a
        /// height off), and ResetAndSeed's own Reset()+Seed loop is about to run for every side layer -
        /// new or pre-existing - right afterward anyway, so seeding here would be redundant at best and a
        /// crash at worst.
        /// </summary>
        void ApplyProfile(PlatformGenerationProfile profile, bool seedNewSideLayers)
        {
            if (profile == null) return;

            platformPrefabs = profile.platformPrefabs;

            profileWeights.Clear();
            if (profile.prefabWeights != null)
            {
                foreach (var pw in profile.prefabWeights)
                {
                    if (pw.prefab != null) profileWeights[pw.prefab] = Mathf.Max(0f, pw.weight);
                }
            }

            forwardSpeed = profile.forwardSpeed;
            launchProfile = profile.launchProfile;
            gravity = profile.gravity;
            maxLaunches = profile.maxLaunches;
            reachabilitySafetyMargin = profile.reachabilitySafetyMargin;

            spawnAheadDistance = profile.spawnAheadDistance;
            despawnBehindDistance = profile.despawnBehindDistance;

            upperBoundOffset = profile.upperBoundOffset;
            maxVerticalStep = profile.maxVerticalStep;

            minGapX = profile.minGapX;
            maxGapX = profile.maxGapX;
            gapChance = profile.gapChance;

            zNoiseMin = profile.zNoiseMin;
            zNoiseMax = profile.zNoiseMax;

            maxSpawnChanceAttempts = profile.maxSpawnChanceAttempts;

            maxConsecutiveSticks = profile.maxConsecutiveSticks;
            maxConsecutiveHardGaps = profile.maxConsecutiveHardGaps;
            guaranteedSafetyInterval = profile.guaranteedSafetyInterval;

            ApplySideLayerConfigs(profile.sideLayers, seedNewSideLayers);

            envelope = LaunchReachability.Calculate(forwardSpeed, launchProfile, gravity, maxLaunches);
        }

        /// <summary>
        /// An empty/null configs leaves sideLayers completely untouched (see PlatformGenerationProfile.sideLayers
        /// for why). Otherwise configs becomes the complete side-layer set, matched against the spawner's
        /// current sideLayers BY LABEL: a config whose label matches an existing layer keeps that PlatformLayer
        /// instance (so its live runtime/history AND its own prefabWeights/overrides survive) and just gets
        /// retuned in place; a config with no matching label gets a brand-new PlatformLayer (its runtime and
        /// weight table are always fresh - see PlatformLayer.runtime). Any existing layer whose label has no
        /// matching config entry is no longer part of the active set and is released outright, rather than being
        /// silently repurposed into an unrelated config the way index-based matching would. Note that
        /// SideLayerConfig doesn't currently carry weight data, so a retuned layer's existing
        /// prefabWeights/overrides are left as-is rather than being overwritten by the profile.
        /// </summary>
        void ApplySideLayerConfigs(PlatformGenerationProfile.SideLayerConfig[] configs, bool seedNewLayers)
        {
            if (configs == null || configs.Length == 0) return;

            PlatformLayer[] existing = sideLayers ?? new PlatformLayer[0];
            // Marks which existing layers got claimed by a config entry, so anything left unclaimed at the
            // end is known to have no home in the new set and gets released rather than retuned.
            bool[] consumed = new bool[existing.Length];

            PlatformLayer[] rebuilt = new PlatformLayer[configs.Length];
            for (int i = 0; i < configs.Length; i++)
            {
                PlatformGenerationProfile.SideLayerConfig config = configs[i];

                int matchIndex = -1;
                for (int j = 0; j < existing.Length; j++)
                {
                    if (consumed[j] || existing[j] == null) continue;
                    if (existing[j].label == config.label) { matchIndex = j; break; }
                }

                bool isNewLayer = matchIndex < 0;
                PlatformLayer layer = isNewLayer ? new PlatformLayer() : existing[matchIndex];
                if (!isNewLayer) consumed[matchIndex] = true;

                layer.label = config.label;
                layer.verticalOffset = config.verticalOffset;
                layer.verticalJitter = config.verticalJitter;
                layer.gapMultiplier = config.gapMultiplier;
                layer.stickChanceBonus = config.stickChanceBonus;
                layer.maxConsecutiveSticks = config.maxConsecutiveSticks;
                layer.clampToReachability = config.clampToReachability;
                layer.prefabOverride = config.prefabOverride;
                layer.gizmoColor = config.gizmoColor;

                rebuilt[i] = layer;

                if (isNewLayer && seedNewLayers)
                {
                    if (player != null && midRuntime.RecordCount > 0)
                        SeedSideLayer(layer);
                    else
                        Debug.LogWarning($"PlatformChunkSpawner: side layer '{layer.label}' was added by a profile before the spawner had seeded its mid layer, so it will stay empty until the next ResetAndSeed.", this);
                }
            }

            // Anything left unconsumed had no matching config entry - it's genuinely gone from the active
            // set, so release its live instances instead of leaving it to be repurposed by index.
            for (int j = 0; j < existing.Length; j++)
            {
                if (!consumed[j] && existing[j] != null) ReleaseAll(existing[j].runtime);
            }

            sideLayers = rebuilt;
        }

        /// <summary>
        /// Manually queues a single pre-made platform block to be inserted as the very next mid-layer
        /// record, positioned flush after whatever platform precedes it - the same mechanism
        /// PlatformProgressionStage.premadeLevel uses internally. Useful for triggering a hand-built
        /// segment from something other than distance (a gameplay event, for instance) without needing to
        /// fabricate a whole PlatformProgressionStage for it.
        /// </summary>
        public void QueuePremadeLevel(PlatformBlock premadeLevel) => pendingPremadeBlock = premadeLevel;

        /// <summary>
        /// Effective MID-LAYER spawn weight for prefab right now: a runtime override if one's been set
        /// via SetPrefabWeight(prefab, weight), else the active profile's weight for it, else
        /// defaultPrefabWeight. Side layers don't use this - see PlatformLayer.GetPrefabWeight.
        /// </summary>
        float GetPrefabWeight(PlatformBlock prefab)
        {
            if (prefab == null) return defaultPrefabWeight;
            if (weightOverrides.TryGetValue(prefab, out float overrideWeight)) return overrideWeight;
            if (profileWeights.TryGetValue(prefab, out float profileWeight)) return profileWeight;
            return defaultPrefabWeight;
        }

        /// <summary>Routes to the mid layer's weight resolution when layer is null, otherwise to that layer's own independent weight table (PlatformLayer.GetPrefabWeight).</summary>
        float WeightFor(PlatformBlock prefab, PlatformLayer layer) =>
            layer != null ? layer.GetPrefabWeight(prefab, defaultPrefabWeight) : GetPrefabWeight(prefab);

        /// <summary>
        /// Overrides prefab's MID-LAYER spawn weight at runtime, on top of whatever the active profile set.
        /// Takes effect on the very next weighted pick and persists across profile swaps until
        /// ClearPrefabWeight is called - this is the spawner-level adjustment knob, independent of which
        /// profile is active. A weight of 0 excludes the prefab from being picked without removing it from
        /// the pool array. For a side layer, use the SetPrefabWeight(PlatformLayer, ...) overload instead -
        /// mid and side layers each resolve weight independently, so this never affects sideLayers.
        /// </summary>
        public void SetPrefabWeight(PlatformBlock prefab, float weight)
        {
            if (prefab == null) return;
            weightOverrides[prefab] = Mathf.Max(0f, weight);
        }

        /// <summary>Removes a mid-layer runtime override set via SetPrefabWeight, reverting prefab to the active profile's weight (or defaultPrefabWeight if unlisted).</summary>
        public void ClearPrefabWeight(PlatformBlock prefab)
        {
            if (prefab == null) return;
            weightOverrides.Remove(prefab);
        }

        /// <summary>
        /// Overrides prefab's spawn weight at runtime for a single side layer, on top of whatever that
        /// layer's own <see cref="PlatformLayer.prefabWeights"/> sets. Effective on the very next weighted
        /// pick for that layer only and persists across profile swaps until ClearPrefabWeight(layer, prefab)
        /// is called - the mid layer and every other side layer are untouched, even if they share the same
        /// prefab. A weight of 0 excludes the prefab from being picked in this layer only.
        /// </summary>
        public void SetPrefabWeight(PlatformLayer layer, PlatformBlock prefab, float weight)
        {
            if (layer == null) { SetPrefabWeight(prefab, weight); return; }
            layer.SetWeightOverride(prefab, weight);
        }

        /// <summary>Removes a runtime override set via SetPrefabWeight(layer, ...), reverting prefab to that layer's own prefabWeights entry (or defaultPrefabWeight if unlisted).</summary>
        public void ClearPrefabWeight(PlatformLayer layer, PlatformBlock prefab)
        {
            if (layer == null) { ClearPrefabWeight(prefab); return; }
            layer.ClearWeightOverride(prefab);
        }

        void SortProgressionStages()
        {
            if (progressionStages == null || progressionStages.Length < 2) return;

            System.Array.Sort(progressionStages, (a, b) =>
                (a?.distanceThreshold ?? float.MaxValue).CompareTo(b?.distanceThreshold ?? float.MaxValue));
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
            PositionPlatform(instance, RecordToWorldX(record.leftEdgeX), record.leftEdgeY, record.zOffset, record.rotationZ);

            Vector3 rightEdge = instance.GetRightEdgeWorld();
            record.rightEdgeX = WorldToRecordX(rightEdge.x);
            record.rightEdgeY = rightEdge.y;
            r.SetRecord(index, record);

            // If this was the pending premade record, its right edge is now known for real (measured off
            // the actual instance, not guessed) - promote it into the side-layer clearance floor so
            // GenerateSideRecord stops placing platforms inside the premade segment's footprint.
            if (r == midRuntime && index == pendingPremadeRecordIndex)
            {
                sideLayerClearanceRightX = record.rightEdgeX;
                pendingPremadeRecordIndex = -1;
            }

            r.liveInstances.Add(instance);
            if (r.liveInstances.Count == 1) r.liveStartIndex = index;
        }

        void MaterializePrepend(PlatformLayerRuntime r, int index)
        {
            PlatformRecord record = r.GetRecord(index);
            PlatformBlock instance = objectPool.Get(record.prefab);
            PositionPlatform(instance, RecordToWorldX(record.leftEdgeX), record.leftEdgeY, record.zOffset, record.rotationZ);

            Vector3 rightEdge = instance.GetRightEdgeWorld();
            record.rightEdgeX = WorldToRecordX(rightEdge.x);
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
            PlatformRecord prev = r.GetRecord(r.LastGlobalIndex);

            // A queued premade level takes priority over normal generation for exactly one record: place
            // it flush (plus premadeLevelGap) after prev and let its own width/anchors carry it, then let
            // ordinary generation resume from its right edge next time this runs.
            if (pendingPremadeBlock != null)
            {
                PlatformBlock premadePrefab = pendingPremadeBlock;
                pendingPremadeBlock = null;

                float premadeLeftEdgeX = prev.rightEdgeX + Mathf.Max(0f, premadeLevelGap);
                r.AddRecord(new PlatformRecord { prefab = premadePrefab, leftEdgeX = premadeLeftEdgeX, leftEdgeY = prev.rightEdgeY, rotationZ = 0f, zOffset = RollZNoise() });

                // Remember this record's index so MaterializeAppend can promote its real measured right
                // edge into sideLayerClearanceRightX once the instance actually exists.
                pendingPremadeRecordIndex = r.LastGlobalIndex;

                // Treat it like a safety checkpoint: whatever stick/hard-gap streak was building up
                // shouldn't carry across a hand-built segment into the next profile's platforms.
                r.consecutiveSticks = 0;
                r.consecutiveHardGaps = 0;
                r.spawnsSinceSafety = 0;
                return;
            }

            r.spawnsSinceSafety++;
            bool forceSafe = r.spawnsSinceSafety >= guaranteedSafetyInterval;

            PlatformBlock prefab = ChooseSpawnablePrefab(platformPrefabs, null, out float missedGap);

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

            r.AddRecord(new PlatformRecord { prefab = prefab, leftEdgeX = spawnLeftEdgeX, leftEdgeY = targetLeftEdgeY, rotationZ = rotationZ, zOffset = RollZNoise() });

            if (forceSafe) r.spawnsSinceSafety = 0;
        }

        void GenerateSideRecord(PlatformLayerRuntime r, PlatformLayer layer)
        {
            PlatformRecord prev = r.GetRecord(r.LastGlobalIndex);
            PlatformBlock[] pool = layer.ResolvePrefabPool(platformPrefabs);
            PlatformBlock prefab = ChooseSpawnablePrefab(pool, layer, out float missedGap);

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

            // Clamp against any premade level currently occupying the mid layer: side layers roll their
            // own independent gap/stick logic and have no other way to know that X-range is spoken for.
            // Mathf.Max is a no-op once the premade's footprint has been passed (sideLayerClearanceRightX
            // stays behind prev.rightEdgeX from then on), so ordinary generation is unaffected outside of
            // an active premade segment.
            float spawnLeftEdgeX = Mathf.Max(prev.rightEdgeX + gapX + missedGap, sideLayerClearanceRightX);
            float rotationZ = prefab.rotation.allowRotation
                ? Random.Range(prefab.rotation.minAngleDegrees, prefab.rotation.maxAngleDegrees)
                : 0f;

            r.AddRecord(new PlatformRecord { prefab = prefab, leftEdgeX = spawnLeftEdgeX, leftEdgeY = targetLeftEdgeY, rotationZ = rotationZ, zOffset = RollZNoise() });
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

        /// <summary>Rolls a random depth offset within [zNoiseMin, zNoiseMax] (order-independent - min/max are sorted before rolling).</summary>
        float RollZNoise()
        {
            float min = Mathf.Min(zNoiseMin, zNoiseMax);
            float max = Mathf.Max(zNoiseMin, zNoiseMax);
            return Random.Range(min, max);
        }

        // ---------------------------------------------------------------
        // Shared helpers
        // ---------------------------------------------------------------

        /// <summary>leftEdgeX/leftEdgeY here are expected to already be real Unity world-space values (see RecordToWorldX). zOffset is added directly on top of the spawner's own Z.</summary>
        void PositionPlatform(PlatformBlock block, float leftEdgeX, float leftEdgeY, float zOffset, float rotationZ)
        {
            block.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, 0f, rotationZ));

            Vector3 currentLeft = block.GetLeftEdgeWorld();
            Vector3 delta = new Vector3(leftEdgeX - currentLeft.x, leftEdgeY - currentLeft.y, zOffset);
            block.transform.position += delta;
        }

        /// <summary>Weighted pick from prefabPool. Pass layer = null for the mid layer (uses GetPrefabWeight -> profile weights -> runtime override -> defaultPrefabWeight); pass the owning PlatformLayer for a side layer so that layer's own prefabWeights/overrides are consulted instead.</summary>
        PlatformBlock ChoosePrefab(PlatformBlock[] prefabPool, PlatformLayer layer)
        {
            float total = 0f;
            foreach (var p in prefabPool) total += WeightFor(p, layer);

            float roll = Random.Range(0f, total);
            float accum = 0f;
            foreach (var p in prefabPool)
            {
                accum += WeightFor(p, layer);
                if (roll <= accum) return p;
            }
            return prefabPool[prefabPool.Length - 1];
        }

        /// <summary>
        /// Weighted-picks a candidate, then rolls its spawnChance. On failure it re-picks and adds that
        /// candidate's width plus a base gap to extraGap - the space it would have occupied becomes part
        /// of the gap before whatever finally gets placed. After maxSpawnChanceAttempts failures in a
        /// row, the last candidate is returned regardless, so a generation step always produces exactly
        /// one record and forward progress is never blocked by an unlucky run of chance rolls. layer is
        /// forwarded to ChoosePrefab unchanged - null for the mid layer, the owning PlatformLayer for a
        /// side layer - so weighting stays scoped to whichever layer is generating.
        /// </summary>
        PlatformBlock ChooseSpawnablePrefab(PlatformBlock[] prefabPool, PlatformLayer layer, out float extraGap)
        {
            extraGap = 0f;
            PlatformBlock prefab = ChoosePrefab(prefabPool, layer);

            for (int attempt = 0; attempt < maxSpawnChanceAttempts; attempt++)
            {
                if (Random.value <= prefab.spawnChance) return prefab;

                extraGap += prefab.Width + minGapX;
                prefab = ChoosePrefab(prefabPool, layer);
            }

            return prefab;
        }

        void OnDrawGizmos()
        {
            if (!showDebugGizmos || player == null) return;

            // Real Unity world-space player position - used for anything drawn directly in the Scene
            // view, as opposed to PlayerProgressX (logical space) which drives the actual spawn/despawn
            // threshold math in AdvanceWindow. Distances (spawnAheadDistance etc.) are translation-invariant
            // between the two frames, so it's only the origin that needs to be the real one here.
            float px = player.position.x;
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
                Vector3 edge = new Vector3(RecordToWorldX(frontier.rightEdgeX), frontier.rightEdgeY, 0f);
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
            Vector3 edge = new Vector3(RecordToWorldX(frontier.rightEdgeX), frontier.rightEdgeY, 0f);

            Gizmos.color = color;
            Gizmos.DrawSphere(edge, 0.1f);
            Color band = color;
            band.a = 0.35f;
            Gizmos.color = band;
            Gizmos.DrawLine(edge + Vector3.down * bandHalfHeight, edge + Vector3.up * bandHalfHeight);
        }
    }
}