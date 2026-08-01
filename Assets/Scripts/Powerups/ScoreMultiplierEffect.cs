using UnityEngine;
using Valley.Scoring;

namespace Valley.Powerups
{
    [CreateAssetMenu(fileName = "ScoreMultiplierEffect", menuName = "Valley/Powerups/Score Multiplier")]
    public class ScoreMultiplierEffect : PowerupEffect
    {
        [Header("Score Multiplier")]
        [Tooltip("Added on top of the tracker's base multiplier and any other active sources.")]
        [SerializeField] private float multiplierBonus = 1f;

        private DistanceScoreTracker targetTracker = null;

        public override void Apply(GameObject target, Transform source)
        {
            if (!target.TryGetComponent<DistanceScoreTracker>(out var tracker)) return;

            targetTracker = tracker;
            targetTracker.SetMultiplierContribution(this, multiplierBonus);
        }

        public override void Revert(GameObject target)
        {
            if (targetTracker == null) return;

            targetTracker.ClearMultiplierContribution(this);
            targetTracker = null;
        }
    }
}