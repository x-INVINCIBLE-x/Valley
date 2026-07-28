using UnityEngine;

namespace Valley.Powerups
{
    public abstract class PowerupEffect : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Icon a UI script can show when this powerup is collected.")]
        public Sprite icon;
        [Tooltip("If true, this effect runs for 'duration' seconds and PowerupReceiver reports normalized progress + calls Revert on expiry. If false, it's an instant effect - a UI script would typically just flash the icon briefly.")]
        public bool isTimed;
        [Tooltip("Only used when isTimed is true.")]
        public float duration = 0f;

        public abstract void Apply(GameObject target, Transform source);

        public virtual void Revert(GameObject target) { }
    }
}
