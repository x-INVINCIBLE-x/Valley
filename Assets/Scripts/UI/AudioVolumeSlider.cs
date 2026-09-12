using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioVolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private AudioMixer audioMixer;

    [Tooltip("The exposed parameter name in the Audio Mixer.")]
    [SerializeField] private string mixerParameter = "MasterVolume";

    [SerializeField] private float defaultValue = 1f;

    private void Awake()
    {
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void Start()
    {
        float savedValue = PlayerPrefs.GetFloat(mixerParameter, defaultValue);

        slider.value = savedValue;

        OnSliderValueChanged(savedValue);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        int percentage = Mathf.RoundToInt(value * 100f);
        percentageText.text = $"{percentage}%";

        float volumeDB = value <= 0.0001f
            ? -80f
            : Mathf.Log10(value) * 20f;

        audioMixer.SetFloat(mixerParameter, volumeDB);

        PlayerPrefs.SetFloat(mixerParameter, value);
        PlayerPrefs.Save();
    }
}