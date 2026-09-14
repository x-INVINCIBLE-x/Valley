using UnityEngine;

[CreateAssetMenu(fileName = "TutorialSlide", menuName = "Tutorial/Tutorial Slide")]
public class TutorialSlideSO : ScriptableObject
{
    [Header("Tutorial Content")]
    public Sprite image;
    public string title;

    [TextArea(3, 8)]
    public string description;
}