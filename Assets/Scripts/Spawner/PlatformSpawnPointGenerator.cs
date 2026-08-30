using System.Collections.Generic;
using UnityEngine;
using Valley.Core.Pooling;
using Valley.Level.Generation;

namespace Valley.Level.Spawning
{
    /// <summary>
    /// Attach to a platform to auto-lay-out and manage its spawn points across any number of categories
    /// (traps, collectibles, etc). Each time this is enabled, every category re-rolls which of its
    /// points are active and spawns from a shared PrefabPoolGroup; each time it's disabled, every
    /// spawned instance for this platform is released back to the pool. A spawn is discarded immediately
    /// if it lands inside another solid collider that isn't part of this platform or the spawned object
    /// itself.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlatformSpawnPointGenerator : MonoBehaviour
    {
        [Header("Anchor Source")]
        [Tooltip("If assigned, Auto-Generate distributes points using this PlatformBlock's computed left/right/surface points, which stay correct even when the platform is rotated. 'Auto-Fill Anchors From PlatformBlock' grabs a sibling PlatformBlock automatically.")]
        public PlatformBlock sourcePlatformBlock;

        [Tooltip("Used instead of sourcePlatformBlock when that's not assigned - plain left/right edge transforms to distribute points between.")]
        public Transform leftAnchor;
        [Tooltip("Used instead of sourcePlatformBlock when that's not assigned.")]
        public Transform rightAnchor;

        [Header("Categories")]
        public List<SpawnPointCategory> categories = new List<SpawnPointCategory>();

        [Header("Overlap Check")]
        [Tooltip("Radius of the physics check run right after placing a spawned object. An overlap with anything else solid here despawns it immediately instead of leaving it embedded.")]
        public float overlapCheckRadius = 0.25f;
        [Tooltip("Which layers count as a blocking overlap.")]
        public LayerMask obstructionLayers = ~0;

        [Header("Debug Gizmos")]
        public bool showGizmos = true;
        public float gizmoRadius = 0.12f;

        PrefabPoolGroup<SpawnedEntity> pool;

        void OnEnable()
        {
            if (pool == null) pool = new PrefabPoolGroup<SpawnedEntity>(transform);
            RerollAndSpawnAll();
        }

        void OnDisable()
        {
            DespawnAll();
        }

        void RerollAndSpawnAll()
        {
            foreach (var category in categories)
                RerollAndSpawnCategory(category);
        }

        void RerollAndSpawnCategory(SpawnPointCategory category)
        {
            if (category.points.Count == 0 || category.prefabs == null || category.prefabs.Length == 0) return;

            var order = new List<int>(category.points.Count);
            for (int i = 0; i < category.points.Count; i++) order.Add(i);
            Shuffle(order);

            int selectCount = Mathf.Clamp(category.activeCount, 0, category.points.Count);
            for (int i = 0; i < selectCount; i++)
            {
                if (Random.value > category.activationProbability) continue;
                SpawnAtPoint(category, category.points[order[i]]);
            }
        }

        static void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        void SpawnAtPoint(SpawnPointCategory category, SpawnPoint point)
        {
            SpawnedEntity prefab = category.prefabs[Random.Range(0, category.prefabs.Length)];
            SpawnedEntity instance = pool.Get(prefab);
            instance.transform.SetPositionAndRotation(transform.TransformPoint(point.localPosition), transform.rotation);

            if (IsBlockedByForeignCollider(instance))
            {
                pool.Release(instance);
                return;
            }

            point.spawnedInstance = instance;
            instance.OnSpawned();
        }

        bool IsBlockedByForeignCollider(SpawnedEntity instance)
        {
            Collider[] hits = Physics.OverlapSphere(instance.transform.position, overlapCheckRadius, obstructionLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform)) continue;
                if (hit.transform.IsChildOf(instance.transform)) continue;
                return true;
            }
            return false;
        }

        void DespawnAll()
        {
            foreach (var category in categories)
            {
                foreach (var point in category.points)
                {
                    if (point.spawnedInstance == null) continue;
                    point.spawnedInstance.OnDespawned();
                    pool.Release(point.spawnedInstance);
                    point.spawnedInstance = null;
                }
            }
        }

        [ContextMenu("Auto-Generate Spawn Points")]
        public void AutoGenerateSpawnPoints()
        {
            if (!TryGetAnchorRange(out float leftX, out float rightX, out float baseY)) return;

            foreach (var category in categories)
            {
                category.points.Clear();
                int count = category.autoPointCount;

                for (int i = 0; i < count; i++)
                {
                    float t = count == 1 ? 0.5f : (float)i / (count - 1);
                    float jitterX = Mathf.Abs(category.positionJitter.x);
                    float jitterY = Mathf.Abs(category.positionJitter.y);
                    float zMin = Mathf.Min(category.zRange.x, category.zRange.y);
                    float zMax = Mathf.Max(category.zRange.x, category.zRange.y);

                    float x = Mathf.Lerp(leftX, rightX, t) + Random.Range(-jitterX, jitterX);
                    float y = baseY + Random.Range(-jitterY, jitterY);
                    float z = Random.Range(zMin, zMax);

                    category.points.Add(new SpawnPoint { localPosition = new Vector3(x, y, z) });
                }
            }
        }

        bool TryGetAnchorRange(out float leftLocalX, out float rightLocalX, out float baseLocalY)
        {
            if (sourcePlatformBlock != null)
            {
                leftLocalX = transform.InverseTransformPoint(sourcePlatformBlock.GetLeftEdgeWorld()).x;
                rightLocalX = transform.InverseTransformPoint(sourcePlatformBlock.GetRightEdgeWorld()).x;
                baseLocalY = transform.InverseTransformPoint(sourcePlatformBlock.GetSurfaceWorld()).y;
                return true;
            }

            if (leftAnchor != null && rightAnchor != null)
            {
                leftLocalX = transform.InverseTransformPoint(leftAnchor.position).x;
                rightLocalX = transform.InverseTransformPoint(rightAnchor.position).x;
                baseLocalY = transform.InverseTransformPoint(Vector3.Lerp(leftAnchor.position, rightAnchor.position, 0.5f)).y;
                return true;
            }

            leftLocalX = 0f;
            rightLocalX = 0f;
            baseLocalY = 0f;
            return false;
        }

        [ContextMenu("Auto-Fill Anchors From PlatformBlock")]
        public void AutoFillAnchorsFromPlatformBlock()
        {
            sourcePlatformBlock = GetComponent<PlatformBlock>();
        }

        void OnDrawGizmos()
        {
            if (!showGizmos || categories == null) return;

            foreach (var category in categories)
            {
                if (category?.points == null) continue;

                Gizmos.color = category.gizmoColor;
                foreach (var point in category.points)
                    Gizmos.DrawSphere(transform.TransformPoint(point.localPosition), gizmoRadius);
            }
        }
    }
}
