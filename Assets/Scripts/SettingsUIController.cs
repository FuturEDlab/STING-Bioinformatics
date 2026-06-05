using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    [Header("Toggles")]
    public Toggle locomotion;
    public Toggle snapTurning;
    public Toggle teleportation;
    public Toggle vignetting;
    public Toggle subtitles;

    [Header("Audio Sliders")]
    public Slider masterVolume;
    public Slider backgroundVolume;
    public Slider narrationVolume;
    public Slider sfxVolume;

    void Start()
    {
        RefreshUI();
    }

    public void OpenSettingsPanel()
    {
        SettingsManager.Instance.BeginTemporaryEdit();
        RefreshUI();
        gameObject.SetActive(true);
    }

    public void CloseSettingsPanel()
    {
        gameObject.SetActive(false);
    }

    void RefreshUI()
    {
        if (SettingsManager.Instance == null) return;
        if (SettingsManager.Instance.CurrentSettings == null) return;

        var settings = SettingsManager.Instance.CurrentSettings;

        locomotion.isOn = settings.locomotionEnabled;
        snapTurning.isOn = settings.snapTurningEnabled;
        teleportation.isOn = settings.teleportationEnabled;
        vignetting.isOn = settings.vignettingEnabled;
        subtitles.isOn = settings.subtitlesEnabled;

        masterVolume.value = settings.masterVolume;
        backgroundVolume.value = settings.backgroundVolume;
        narrationVolume.value = settings.narrationVolume;
        sfxVolume.value = settings.sfxVolume;
    }

    public void OnSaveButtonClicked()
    {
        SettingsManager.Instance.Save();
        CloseSettingsPanel();
    }

    public void OnExitButtonClicked()
    {
        SettingsManager.Instance.CancelTemporaryEdit();
        RefreshUI();
        CloseSettingsPanel();
    }
}