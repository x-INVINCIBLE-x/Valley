using UnityEngine;

namespace Valley.Theming
{
    [CreateAssetMenu(fileName = "ThemeDefinition", menuName = "Valley/Theme Definition")]
    public class ThemeDefinition : ScriptableObject
    {
        [Header("Info")]
        [SerializeField] private string saveId;

        public string themeName;
        public Sprite icon;

        [Header("Cost")]
        public int price;

        [Header("Skybox")]
        public Material skyboxMaterial;
        public Color ambientLightColor = Color.white;
        [Range(0f, 8f)] public float ambientIntensity = 1f;

        [Header("Fog")]
        public bool fogEnabled = true;
        public Color fogColor = Color.gray;
        public FogMode fogMode = FogMode.ExponentialSquared;

        // Used when fogMode is Linear
        public float fogStartDistance = 0f;
        public float fogEndDistance = 300f;

        // Used when fogMode is Exponential or ExponentialSquared
        [Range(0f, 0.5f)] public float fogDensity = 0.002f;
        
        
        public string SaveId => saveId;
    }
}