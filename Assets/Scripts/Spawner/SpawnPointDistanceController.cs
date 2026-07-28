using UnityEngine;

namespace Valley.Level.Spawning
{
    /// <summary>
    /// Sits alongside a PlatformSpawnPointGenerator and re-tunes its category settings at runtime as a
    /// function of distance traveled. Distance tracking itself isn't wired up yet - GetCurrentDistance()
    /// is a placeholder returning 0 until the game's actual progress/distance source is hooked in here.
    /// Changes apply to the category data only; they take effect the next time the platform is
    /// (re)enabled, same as any other category setting.
    /// </summary>
    [RequireComponent(typeof(PlatformSpawnPointGenerator))]
    public class SpawnPointDistanceController : MonoBehaviour
    {
        [Header("Update Timing")]
        [Tooltip("How often (seconds) this re-evaluates and applies distance-based adjustments.")]
        public float updateInterval = 0.5f;

        [Header("Distance Normalization")]
        [Tooltip("Distance value that maps to 1.0 on the curves below. Only affects curve evaluation - has no bearing on the (not yet implemented) distance calculation itself.")]
        public float normalizedDistanceRange = 1000f;

        [Header("Per-Category Scaling")]
        [Tooltip("Index must line up with the generator's Categories list.")]
        public DistanceScaling[] categoryScaling = new DistanceScaling[0];

        PlatformSpawnPointGenerator generator;
        float timeSinceLastUpdate;

        [System.Serializable]
        public struct DistanceScaling
        {
            [Tooltip("Maps normalized distance (0-1) to this category's activeCount.")]
            public AnimationCurve activeCountOverDistance;
            [Tooltip("Maps normalized distance (0-1) to this category's activationProbability.")]
            public AnimationCurve activationProbabilityOverDistance;
        }

        void Awake()
        {
            generator = GetComponent<PlatformSpawnPointGenerator>();
        }

        void Update()
        {
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate < updateInterval) return;
            timeSinceLastUpdate = 0f;

            ApplyDistanceScaling(GetCurrentDistance());
        }

        /// <summary>Placeholder - always returns 0 until the game's distance/progress tracking is wired in here.</summary>
        float GetCurrentDistance()
        {
            return 0f;
        }

        void ApplyDistanceScaling(float distance)
        {
            if (generator == null || generator.categories == null || categoryScaling == null) return;

            float normalized = normalizedDistanceRange <= 0f ? 0f : Mathf.Clamp01(distance / normalizedDistanceRange);
            int count = Mathf.Min(generator.categories.Count, categoryScaling.Length);

            for (int i = 0; i < count; i++)
            {
                var category = generator.categories[i];
                var scaling = categoryScaling[i];

                if (scaling.activeCountOverDistance != null && scaling.activeCountOverDistance.length > 0)
                    category.activeCount = Mathf.RoundToInt(scaling.activeCountOverDistance.Evaluate(normalized));

                if (scaling.activationProbabilityOverDistance != null && scaling.activationProbabilityOverDistance.length > 0)
                    category.activationProbability = Mathf.Clamp01(scaling.activationProbabilityOverDistance.Evaluate(normalized));
            }
        }
    }
}
