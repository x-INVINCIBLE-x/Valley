using UnityEngine;

namespace Valley.Platforms
{
    [RequireComponent(typeof(Collider))]
    public class PlatformEffectZone : MonoBehaviour
    {
        [SerializeField] private PlatformEffectProfile profile;

        public PlatformEffectProfile Profile => profile;
    }
}
