using UnityEngine;
using Valley.Core;

namespace Valley.Powerups
{
    [CreateAssetMenu(
        fileName = "IncreaseMaxChargesEffect",
        menuName = "Valley/Powerups/Increase Max Charges"
    )]
    public class IncreaseMaxChargesEffect : PowerupEffect
    {
        [Header("Charges")]
        [Tooltip("Amount added to the player's maximum launch charges.")]
        [SerializeField] private int amount = 1;

        public override void Apply(GameObject target, Transform source)
        {
            var launchGate = target.GetComponent<PlayerLaunchGate>();
            if (launchGate == null) return;

            launchGate.IncreaseMaxCharges(amount);
        }

        public override void Revert(GameObject target)
        {
            var launchGate = target.GetComponent<PlayerLaunchGate>();
            if (launchGate == null) return;

            launchGate.DecreaseMaxCharges(amount);
        }
    }
}