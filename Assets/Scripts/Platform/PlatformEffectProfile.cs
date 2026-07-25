using UnityEngine;

namespace Valley.Platforms
{
    [CreateAssetMenu(fileName = "PlatformEffectProfile", menuName = "Valley/Platform Effect Profile")]
    public class PlatformEffectProfile : ScriptableObject
    {
        public float gravityMultiplier = 1f;
        public float bounceMultiplier = 1f;
        public float speedMultiplier = 1f;
    }
}
