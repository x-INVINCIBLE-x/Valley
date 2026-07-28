using UnityEngine;

namespace Valley.Core
{
    [CreateAssetMenu(fileName = "LaunchProfile", menuName = "Valley/Launch Profile")]
    public class LaunchProfile : ScriptableObject
    {
        public float minLaunchForce = 5f;
        public float maxLaunchForce = 20f;
        public AnimationCurve forceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Range(0f, 1f)] public float previousVelocityRetention = 0.2f;

        public float EvaluateForce(float charge)
        {
            return Mathf.Lerp(minLaunchForce, maxLaunchForce, forceCurve.Evaluate(charge));
        }
    }
}