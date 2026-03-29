using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button backButton;
    [SerializeField] private UnityEvent onBack;

    private void OnEnable()
    {
        float saved = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(saved);
            masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        AudioListener.volume = saved;

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDisable()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    private void OnBackClicked()
    {
        gameObject.SetActive(false);
        onBack?.Invoke();
    }
}
