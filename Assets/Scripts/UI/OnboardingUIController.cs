using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoreMountains.Feedbacks;

public class OnboardingUIController : MonoBehaviour
{
    [Header("Name Input")]
    [SerializeField] private TMP_InputField nameInputField;

    [Header("Age Slider")]
    [SerializeField] private Slider ageSlider;
    [SerializeField] private TextMeshProUGUI ageValueText;
    [SerializeField] private int minAge = 1;
    [SerializeField] private int maxAge = 100;
    [SerializeField] private int defaultAge = 18;

    [Header("Gender Toggles")]
    [SerializeField] private Toggle maleToggle;
    [SerializeField] private Toggle femaleToggle;
    [SerializeField] private Toggle otherToggle;

    [Header("Submit")]
    [SerializeField] private Button submitButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private MMF_Player onSuccessfulSubmit;

    private string selectedGender = string.Empty;

    private void Awake()
    {
        if (nameInputField != null)
        {
            nameInputField.onValidateInput = ValidateNameInput;
        }
    }

    private void Start()
    {
        if (ageSlider != null)
        {
            ageSlider.minValue = minAge;
            ageSlider.maxValue = maxAge;
            ageSlider.wholeNumbers = true;
            ageSlider.value = defaultAge;
            ageSlider.onValueChanged.AddListener(UpdateAgeText);
            UpdateAgeText(ageSlider.value);
        }

        if (maleToggle != null)
            maleToggle.onValueChanged.AddListener(isOn => OnGenderToggleChanged(maleToggle, "Male", isOn));
        if (femaleToggle != null)
            femaleToggle.onValueChanged.AddListener(isOn => OnGenderToggleChanged(femaleToggle, "Female", isOn));
        if (otherToggle != null)
            otherToggle.onValueChanged.AddListener(isOn => OnGenderToggleChanged(otherToggle, "Other", isOn));

        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmit);

        ShowStatus(string.Empty);
    }

    private void OnDestroy()
    {
        if (ageSlider != null) ageSlider.onValueChanged.RemoveListener(UpdateAgeText);
        if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmit);
    }

    // --- Name validation -------------------------------------------------

    /// <summary>
    /// Called by TMP_InputField for every keystroke, BEFORE the character is
    /// added. Returning '\0' rejects the character entirely, so invalid
    /// characters never appear in the field in the first place.
    /// </summary>
    private char ValidateNameInput(string text, int charIndex, char addedChar)
    {
        // Letters only. If you want to allow spaces for multi-word names,
        // change the condition to: char.IsLetter(addedChar) || addedChar == ' '
        if (char.IsLetter(addedChar))
            return addedChar;

        return '\0';
    }

    // --- Age slider --------------------------------------------------------

    private void UpdateAgeText(float value)
    {
        int ageValue = Mathf.RoundToInt(value);
        if (ageValueText != null)
            ageValueText.text = ageValue.ToString();
    }

    // --- Gender toggles ------------------------------------------------

    private void OnGenderToggleChanged(Toggle changedToggle, string genderLabel, bool isOn)
    {
        // Ignore the "turned off" callback fired on the toggle that lost selection.
        if (!isOn) return;

        selectedGender = genderLabel;

        // Safety net: force the others off even if a ToggleGroup wasn't assigned.
        if (maleToggle != null && maleToggle != changedToggle) maleToggle.isOn = false;
        if (femaleToggle != null && femaleToggle != changedToggle) femaleToggle.isOn = false;
        if (otherToggle != null && otherToggle != changedToggle) otherToggle.isOn = false;
    }

    // --- Submit ------------------------------------------------------------

    private void OnSubmit()
    {
        string playerName = nameInputField != null ? nameInputField.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(playerName))
        {
            ShowStatus("Please enter a name.");
            return;
        }

        if (string.IsNullOrEmpty(selectedGender))
        {
            ShowStatus("Please select a gender.");
            return;
        }

        int age = ageSlider != null ? Mathf.RoundToInt(ageSlider.value) : defaultAge;

        UserOnboardingData.SetData(playerName, age, selectedGender);
        ShowStatus("");

        onSuccessfulSubmit.PlayFeedbacks();
    }

    private void ShowStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}