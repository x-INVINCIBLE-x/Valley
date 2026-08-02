using UnityEngine;

namespace Valley.Paths
{
    [CreateAssetMenu(fileName = "PathDefinition", menuName = "Valley/Path Definition")]
    public class PathDefinition : ScriptableObject
    {
        [Tooltip("The Z position this path occupies.")]
        public float zPosition;
        [Tooltip("Color identifying this path everywhere it's used - teleporters, path visuals, UI, etc.")]
        public Color pathColor = Color.white;
    }
}