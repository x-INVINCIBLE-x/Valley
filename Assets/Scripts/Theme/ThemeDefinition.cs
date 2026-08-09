using UnityEngine;

namespace Valley.Theming
{
    [CreateAssetMenu(fileName = "ThemeDefinition", menuName = "Valley/Theme Definition")]
    public class ThemeDefinition : ScriptableObject
    {
        public string themeName;
        public Sprite icon;
        public int price;
    }
}