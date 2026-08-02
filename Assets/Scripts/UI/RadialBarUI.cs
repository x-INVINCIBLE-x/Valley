using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

public class RadialBarUI : MonoBehaviour
{
    [SerializeField] private MMProgressBar radialBar;
    [SerializeField] private Image[] icons;

    public void UpdateIcon(Sprite sprite)
    {
        for (int i = 0; i < icons.Length; i++)
        {
            icons[i].sprite = sprite;
        }
    }

    public void UpdateRadialBar(float current, float max)
    {
        radialBar.UpdateBar(current, 0f, max);
    }
}
