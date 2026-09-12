using System.Collections;
using UnityEngine;
using Valley.Core.Pooling;

namespace Valley.Level.Environment
{
    public class SkyLanternSpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private SkyLanternMover lanternPrefab;

        [Header("Spawn Volume")]
        [SerializeField] private Vector3 volumeSize = new Vector3(10f, 5f, 10f);

        [Header("Spawn")]
        [SerializeField, Min(0f)] private float spawnRate = 2f;
        [SerializeField, Min(1)] private int maxAlive = 20;

        [Header("Spawn Exclusion")]
        [SerializeField] private bool excludeZRange = true;
        [SerializeField] private float excludedZMin = -2f;
        [SerializeField] private float excludedZMax = 2f;

        [Header("Lifetime")]
        [SerializeField, Min(0f)] private float minLifetime = 8f;
        [SerializeField, Min(0f)] private float maxLifetime = 15f;

        private PrefabPoolGroup<SkyLanternMover> pool;
        private int aliveCount;

        private void Awake()
        {
            if (lanternPrefab == null)
            {
                Debug.LogError($"{name}: Lantern prefab is not assigned.", this);
                enabled = false;
                return;
            }

            pool = new PrefabPoolGroup<SkyLanternMover>(transform);
        }

        private void OnEnable()
        {
            StartCoroutine(SpawnRoutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private IEnumerator SpawnRoutine()
        {
            // Optional initial delay so everything does not appear instantly.
            if (spawnRate > 0f)
                yield return new WaitForSeconds(Random.Range(0f, 1f));

            while (enabled)
            {
                if (aliveCount < maxAlive)
                    SpawnLantern();

                float delay;

                if (spawnRate <= 0f)
                {
                    delay = 1f;
                }
                else
                {
                    // Slightly randomize the interval so spawning feels organic.
                    float baseDelay = 1f / spawnRate;
                    delay = baseDelay * Random.Range(0.75f, 1.25f);
                }

                yield return new WaitForSeconds(delay);
            }
        }

        private void SpawnLantern()
        {
            SkyLanternMover lantern = pool.Get(lanternPrefab);

            Vector3 spawnPosition = GetRandomSpawnPosition();

            lantern.transform.position = spawnPosition;
            lantern.transform.rotation = new Quaternion(
                lantern.transform.rotation.x, Random.value, lantern.transform.rotation.z, Random.value);

            lantern.transform.SetParent(null);

            float lifetime = Random.Range(minLifetime, maxLifetime);

            lantern.Initialize(
                lifetime,
                OnLanternReleased);

            aliveCount++;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 halfSize = volumeSize * 0.5f;

            for (int attempt = 0; attempt < 100; attempt++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-halfSize.x, halfSize.x),
                    Random.Range(-halfSize.y, halfSize.y),
                    Random.Range(-halfSize.z, halfSize.z));

                // Reject positions inside the excluded Z section.
                if (excludeZRange &&
                    offset.z >= excludedZMin &&
                    offset.z <= excludedZMax)
                {
                    continue;
                }

                return transform.position + offset;
            }

            // Fallback.
            return transform.position + new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y),
                halfSize.z);
        }

        private void OnLanternReleased(SkyLanternMover lantern)
        {
            aliveCount = Mathf.Max(0, aliveCount - 1);
            pool.Release(lantern);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 halfSize = volumeSize * 0.5f;

            // Full spawn volume.
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(transform.position, volumeSize);

            if (!excludeZRange)
                return;

            // Clamp the excluded range to the spawn volume.
            float minZ = Mathf.Max(excludedZMin, -halfSize.z);
            float maxZ = Mathf.Min(excludedZMax, halfSize.z);

            if (minZ >= maxZ)
                return;

            // Excluded Z slab.
            float excludedDepth = maxZ - minZ;
            float excludedCenterZ = (minZ + maxZ) * 0.5f;

            Vector3 excludedCenter = transform.position + new Vector3(
                0f,
                0f,
                excludedCenterZ);

            Vector3 excludedSize = new Vector3(
                volumeSize.x,
                volumeSize.y,
                excludedDepth);

            Gizmos.DrawWireCube(excludedCenter, excludedSize);
        }
    }
}