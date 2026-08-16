using System.Collections.Generic;
using UnityEngine;

namespace Valley.Level.Generation
{
    public enum PlatformSpawnMode
    {
        /// <summary>Walks prefabSet in order, looping back to the start (or stopping) at the end.</summary>
        Sequential,

        /// <summary>Draws from a ShuffleBag: every prefab in prefabSet spawns once before any repeat.</summary>
        RandomBag
    }

    public enum SpawnOffsetMode
    {
        /// <summary>spawnOffset is added as-is, in world space.</summary>
        Absolute,

        /// <summary>spawnOffset is rotated into referenceObject's local space before being added, so it turns/moves with it.</summary>
        Relative
    }

    public enum StreamAxis { X, Y, Z }

    /// <summary>
    /// Streams a chain of PlatformBlock instances along one world axis, keeping everything within a
    /// window around referenceObject spawned and everything outside it despawned.
    ///
    /// Each position in the chain is a "slot" (0, 1, 2, ...). Slot -> prefab is decided either
    /// Sequential (prefabSet[slot % count]) or RandomBag (a ShuffleBag draw). Once a slot has been
    /// generated its prefab choice, spawn/gap roll, attach roll and final world position/rotation are
    /// cached in a per-slot SpawnRecord, so moving the reference object backward re-shows the exact same
    /// content instead of rolling new randomness - as long as that slot is still within historyLimit
    /// slots of the active window. Slots evicted past that limit are forgotten and get freshly
    /// regenerated (new rolls) if the reference object backs up into them again.
    ///
    /// Chaining reuses PlatformBlock.GetLeftEdgeWorld()/GetRightEdgeWorld() after instantiating, so
    /// anchors, rotation clamps and per-prefab attach settings are respected automatically. spawnChance
    /// is rolled per slot too - a failed roll leaves that slot as an empty gap instead of retrying with a
    /// different prefab, matching PlatformBlock's own documented behaviour.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlatformSequenceSpawner : MonoBehaviour
    {
        [Header("Reference")]
        [Tooltip("The object whose position drives spawning/despawning - typically the player or camera.")]
        public Transform referenceObject;

        [Tooltip("World axis the platform chain is laid out along.")]
        public StreamAxis streamAxis = StreamAxis.X;

        [Header("Prefab Set")]
        [Tooltip("The curated set of platform prefabs this spawner is allowed to place.")]
        public List<PlatformBlock> prefabSet = new List<PlatformBlock>();

        [Header("Spawn Mode")]
        public PlatformSpawnMode spawnMode = PlatformSpawnMode.Sequential;

        [Tooltip("Sequential mode only: loop back to the start of prefabSet once the end is reached. If false, the chain simply stops growing once the list is exhausted.")]
        public bool loopSequence = true;

        [Tooltip("Random Bag mode only: 0 = time-based seed. Any other value gives a reproducible draw order.")]
        public int randomSeed = 0;

        [Header("Streaming Window")]
        [Tooltip("How far ahead of the reference object (along Stream Axis) the chain stays generated/spawned.")]
        [Min(0f)] public float aheadDistance = 20f;

        [Tooltip("How far behind the reference object (along Stream Axis) already-placed platforms stay spawned before despawning.")]
        [Min(0f)] public float behindDistance = 10f;

        [Header("Gaps")]
        [Tooltip("Gap distance rolled between two blocks whenever a flush-attach attempt fails.")]
        public Vector2 gapDistanceRange = new Vector2(1f, 2.5f);

        [Header("Offset")]
        public SpawnOffsetMode offsetMode = SpawnOffsetMode.Relative;
        [Tooltip("Extra offset baked into every newly generated slot. Absolute = fixed world-space vector. Relative = expressed in referenceObject's local space, so it turns/moves with it. Already-generated slots are not retroactively moved when this changes.")]
        public Vector3 spawnOffset = Vector3.zero;

        [Header("History")]
        [Tooltip("How many despawned slots behind the active window are remembered exactly (prefab, spawn/gap roll, attach roll, position). Slots older than this are forgotten and re-rolled fresh if the reference object backs up into them again.")]
        [Min(0)] public int historyLimit = 30;

        [Header("Start")]
        [Tooltip("World-space transform position used for slot 0. Every later slot chains off the measured edges of the slot before/after it.")]
        public Vector3 originPosition = Vector3.zero;

        [System.Serializable]
        public class SpawnRecord
        {
            public int slot;
            public int prefabIndex;
            public bool spawns;   // false = deliberate empty gap (spawnChance roll failed)
            public bool active;   // currently inside the streaming window (used for history eviction)
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 leftEdgeWorld;
            public Vector3 rightEdgeWorld;
            [System.NonSerialized] public PlatformBlock instance;
        }

        readonly Dictionary<int, SpawnRecord> timeline = new Dictionary<int, SpawnRecord>();
        ShuffleBag<int> shuffleBag;
        Transform container;
        bool hasGenerated;
        int highestGeneratedSlot = -1;
        int lowestRetainedSlot;

        void Awake()
        {
            container = new GameObject("SpawnedPlatforms").transform;
            container.SetParent(transform, false);

            InitRandomBagIfNeeded();
        }

        void Update()
        {
            if (referenceObject == null || prefabSet == null || prefabSet.Count == 0) return;
            RefreshStreaming();
        }

        void InitRandomBagIfNeeded()
        {
            if (spawnMode != PlatformSpawnMode.RandomBag || prefabSet == null || prefabSet.Count == 0) return;

            var indices = new List<int>(prefabSet.Count);
            for (int i = 0; i < prefabSet.Count; i++) indices.Add(i);
            shuffleBag = new ShuffleBag<int>(indices, randomSeed);
        }

        /// <summary>Destroys every spawned instance and clears all history, ready to start the chain over from slot 0.</summary>
        public void ResetSpawner()
        {
            foreach (var record in timeline.Values)
                if (record.instance != null) Destroy(record.instance.gameObject);

            timeline.Clear();
            hasGenerated = false;
            highestGeneratedSlot = -1;
            lowestRetainedSlot = 0;

            if (spawnMode == PlatformSpawnMode.RandomBag) InitRandomBagIfNeeded();
        }

        void RefreshStreaming()
        {
            float refAxis = GetAxisValue(referenceObject.position);
            float windowStart = refAxis - behindDistance;
            float windowEnd = refAxis + aheadDistance;

            // Grow the forward frontier until the newest slot's right edge covers the window's leading edge.
            while (!hasGenerated || GetAxisValue(timeline[highestGeneratedSlot].rightEdgeWorld) < windowEnd)
            {
                if (!GenerateSlot(hasGenerated ? highestGeneratedSlot + 1 : 0, extendingForward: true)) break;
            }

            // Grow the backward frontier (regenerating any forgotten history) until it covers the window's trailing edge.
            while (hasGenerated && lowestRetainedSlot > 0 &&
                   GetAxisValue(timeline[lowestRetainedSlot].leftEdgeWorld) > windowStart)
            {
                if (!GenerateSlot(lowestRetainedSlot - 1, extendingForward: false)) break;
            }

            if (!hasGenerated) return;

            // Sync every retained slot's spawned/despawned state with the current window.
            for (int slot = lowestRetainedSlot; slot <= highestGeneratedSlot; slot++)
            {
                SpawnRecord record = timeline[slot];
                bool shouldBeActive = GetAxisValue(record.rightEdgeWorld) >= windowStart &&
                                       GetAxisValue(record.leftEdgeWorld) <= windowEnd;
                record.active = shouldBeActive;

                if (shouldBeActive && record.spawns && record.instance == null) InstantiateRecord(record);
                else if (!shouldBeActive && record.instance != null) DestroyRecordInstance(record);
            }

            TrimHistory();
        }

        bool GenerateSlot(int slot, bool extendingForward)
        {
            if (slot < 0) return false;

            int prefabIndex = ResolvePrefabIndex(slot);
            if (prefabIndex < 0) return false; // non-looping sequential mode ran out

            PlatformBlock prefab = prefabSet[prefabIndex];
            bool spawns = Random.value <= Mathf.Clamp01(prefab.spawnChance);

            SpawnRecord neighbor = null;
            if (extendingForward) timeline.TryGetValue(slot - 1, out neighbor);
            else timeline.TryGetValue(slot + 1, out neighbor);

            var record = new SpawnRecord { slot = slot, prefabIndex = prefabIndex, spawns = spawns };
            Vector3 axisDir = GetAxisDirection();

            if (spawns)
            {
                Quaternion rotation = Quaternion.identity;
                if (prefab.rotation.allowRotation)
                {
                    float angle = Random.Range(prefab.rotation.minAngleDegrees, prefab.rotation.maxAngleDegrees);
                    rotation = Quaternion.Euler(0f, 0f, angle);
                }

                Vector3 provisionalPosition = neighbor != null ? neighbor.position : ApplyOffset(originPosition);
                PlatformBlock instance = Instantiate(prefab, provisionalPosition, rotation, container);

                if (neighbor == null)
                {
                    instance.transform.position = ApplyOffset(originPosition);
                }
                else
                {
                    bool flush = neighbor.spawns && RollAttach(
                        extendingForward ? neighbor.prefabIndex : prefabIndex,
                        extendingForward ? prefabIndex : neighbor.prefabIndex);
                    float gap = flush ? 0f : Random.Range(gapDistanceRange.x, gapDistanceRange.y);

                    if (extendingForward)
                    {
                        Vector3 targetLeftEdge = neighbor.rightEdgeWorld + axisDir * gap;
                        instance.transform.position += targetLeftEdge - instance.GetLeftEdgeWorld();
                    }
                    else
                    {
                        Vector3 targetRightEdge = neighbor.leftEdgeWorld - axisDir * gap;
                        instance.transform.position += targetRightEdge - instance.GetRightEdgeWorld();
                    }
                }

                record.position = instance.transform.position;
                record.rotation = instance.transform.rotation;
                record.leftEdgeWorld = instance.GetLeftEdgeWorld();
                record.rightEdgeWorld = instance.GetRightEdgeWorld();
                record.instance = instance;
            }
            else
            {
                // Empty gap: nothing to instantiate, so the footprint is estimated from the prefab
                // asset's own bounds (rotation isn't applied since there's no instance to rotate).
                float width = prefab.Width;
                float gap = Random.Range(gapDistanceRange.x, gapDistanceRange.y);

                if (neighbor == null)
                {
                    record.position = ApplyOffset(originPosition);
                    record.leftEdgeWorld = record.position - axisDir * (width * 0.5f);
                    record.rightEdgeWorld = record.position + axisDir * (width * 0.5f);
                }
                else if (extendingForward)
                {
                    record.leftEdgeWorld = neighbor.rightEdgeWorld + axisDir * gap;
                    record.rightEdgeWorld = record.leftEdgeWorld + axisDir * width;
                    record.position = (record.leftEdgeWorld + record.rightEdgeWorld) * 0.5f;
                }
                else
                {
                    record.rightEdgeWorld = neighbor.leftEdgeWorld - axisDir * gap;
                    record.leftEdgeWorld = record.rightEdgeWorld - axisDir * width;
                    record.position = (record.leftEdgeWorld + record.rightEdgeWorld) * 0.5f;
                }
            }

            timeline[slot] = record;
            if (!hasGenerated || slot > highestGeneratedSlot) highestGeneratedSlot = slot;
            if (!hasGenerated || slot < lowestRetainedSlot) lowestRetainedSlot = slot;
            hasGenerated = true;
            return true;
        }

        int ResolvePrefabIndex(int slot)
        {
            if (prefabSet.Count == 0) return -1;

            if (spawnMode == PlatformSpawnMode.Sequential)
            {
                if (!loopSequence && slot >= prefabSet.Count) return -1;
                return slot % prefabSet.Count;
            }

            if (shuffleBag == null) InitRandomBagIfNeeded();
            return shuffleBag.Draw();
        }

        bool RollAttach(int leftPrefabIndex, int rightPrefabIndex)
        {
            PlatformBlock left = prefabSet[leftPrefabIndex];
            PlatformBlock right = prefabSet[rightPrefabIndex];
            if (!left.rightAttach.allowed || !right.leftAttach.allowed) return false;

            float successChance = (left.rightAttach.successRate + right.leftAttach.successRate) * 0.5f;
            return Random.value <= successChance;
        }

        void InstantiateRecord(SpawnRecord record)
        {
            if (!record.spawns || record.instance != null) return;
            record.instance = Instantiate(prefabSet[record.prefabIndex], record.position, record.rotation, container);
        }

        void DestroyRecordInstance(SpawnRecord record)
        {
            if (record.instance == null) return;
            Destroy(record.instance.gameObject);
            record.instance = null;
        }

        void TrimHistory()
        {
            int tail = 0;
            for (int slot = lowestRetainedSlot; slot <= highestGeneratedSlot; slot++)
            {
                if (timeline[slot].active) break;
                tail++;
            }

            int excess = tail - historyLimit;
            for (int i = 0; i < excess; i++)
            {
                timeline.Remove(lowestRetainedSlot);
                lowestRetainedSlot++;
            }
        }

        Vector3 ApplyOffset(Vector3 basePosition) => basePosition + GetOffsetVector();

        Vector3 GetOffsetVector()
        {
            if (offsetMode == SpawnOffsetMode.Absolute || referenceObject == null) return spawnOffset;
            return referenceObject.TransformDirection(spawnOffset);
        }

        Vector3 GetAxisDirection()
        {
            switch (streamAxis)
            {
                case StreamAxis.X: return Vector3.right;
                case StreamAxis.Y: return Vector3.up;
                default: return Vector3.forward;
            }
        }

        float GetAxisValue(Vector3 v)
        {
            switch (streamAxis)
            {
                case StreamAxis.X: return v.x;
                case StreamAxis.Y: return v.y;
                default: return v.z;
            }
        }

        void OnValidate()
        {
            if (gapDistanceRange.y < gapDistanceRange.x) gapDistanceRange.y = gapDistanceRange.x;
        }

        void OnDrawGizmosSelected()
        {
            if (referenceObject == null) return;

            Vector3 axisDir = GetAxisDirection();
            Vector3 refPos = referenceObject.position;

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
            Gizmos.DrawLine(refPos - axisDir * behindDistance + Vector3.up * 2f, refPos - axisDir * behindDistance - Vector3.up * 2f);

            Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
            Gizmos.DrawLine(refPos + axisDir * aheadDistance + Vector3.up * 2f, refPos + axisDir * aheadDistance - Vector3.up * 2f);
        }
    }
}