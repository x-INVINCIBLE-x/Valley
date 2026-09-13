using UnityEngine;
using Valley.Scoring;

namespace Valley.Powerups
{
    [CreateAssetMenu(fileName = "ScoreMultiplierEffect", menuName = "Valley/Powerups/Score Multiplier")]
    public class ScoreMultiplierEffect : PowerupEffect
    {
        [Header("Score Multiplier")]
        [Tooltip("Added on top of the tracker's base multiplier and any other active sources.")]
        [SerializeField] private int minMultiplierBonus = 2;
        [SerializeField] private int maxMultiplierBonus = 5;

        private DistanceScoreTracker targetTracker = null;

        public override void Apply(GameObject target, Transform source)
        {
            targetTracker = DistanceScoreTracker.Instance;
            if (targetTracker == null) return;

            int multiplierBonus = Random.Range(minMultiplierBonus, maxMultiplierBonus + 1);
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