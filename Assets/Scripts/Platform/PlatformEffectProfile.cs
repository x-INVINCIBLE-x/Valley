using UnityEngine;

namespace Valley.Platforms
{
    public enum EffectType
    {
        Base,
        Gravity,
        Bounce,
        Speed
    }

    [CreateAssetMenu(fileName = "PlatformEffectProfile", menuName = "Valley/Platform Effect Profile")]
    public class PlatformEffectProfile : ScriptableObject
    {
        public EffectType EffectType;
        public float gravityMultiplier = 1f;
        public float bounceMultiplier = 1f;
        public float speedMultiplier = 1f;
    }
}
