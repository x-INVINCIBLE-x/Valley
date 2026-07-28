using UnityEngine;
using Valley.Combat;

namespace Valley.Powerups
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "Valley/Powerups/Heal")]
    public class HealEffect : PowerupEffect
    {
        [Header("Heal")]
        [Tooltip("Amount of health restored.")]
        [SerializeField] private float healAmount = 25f;

        public override void Apply(GameObject target, Transform source)
        {
            var health = target.GetComponent<Health>();
            if (health == null) return;

            health.Heal(healAmount);
        }
    }
}
