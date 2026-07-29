using UnityEngine;

namespace Valley.QTE
{
    [CreateAssetMenu(fileName = "QuickTimeEventProfile", menuName = "Valley/Quick Time Event Profile")]
    public class QuickTimeEventProfile : ScriptableObject
    {
        [Header("Requirements")]
        [Tooltip("Number of taps needed to escape.")]
        public int requiredTaps = 5;
        [Tooltip("Seconds allowed to reach requiredTaps before the QTE fails.")]
        public float duration = 3f;
    }
}