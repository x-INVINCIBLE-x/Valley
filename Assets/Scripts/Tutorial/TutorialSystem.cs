using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialSystem : MonoBehaviour
{
    [Header("Tutorial Slides")]
    [SerializeField] private TutorialSlideSO[] slides;

    [Header("UI References")]
    [SerializeField] private Image tutorialImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    private int currentSlideIndex = 0;

    private void Start()
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("TutorialSystem: No tutorial slides assigned.");
            UpdateButtons();
            return;
        }

        ShowSlide(0);
    }

    public void NextSlide()
    {
        if (currentSlideIndex >= slides.Length - 1)
            return;

        currentSlideIndex++;
        ShowSlide(currentSlideIndex);
    }

    public void PreviousSlide()
    {
        if (currentSlideIndex <= 0)
            return;

        currentSlideIndex--;
        ShowSlide(currentSlideIndex);
    }

    private void ShowSlide(int index)
    {
        if (slides == null || slides.Length == 0)
            return;

        if (index < 0 || index >= slides.Length)
            return;

        TutorialSlideSO slide = slides[index];

        tutorialImage.sprite = slide.image;
        titleText.text = slide.title;
        descriptionText.text = slide.description;

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (previousButton != null)
            previousButton.interactable = currentSlideIndex > 0;

        if (nextButton != null)
            nextButton.interactable =
                slides != null && currentSlideIndex < slides.Length - 1;
    }
}