using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
// using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer mainAudioMixer;

    [Header("Optional UI (Settings Menu)")]
    public Slider masterSlider;
    public Slider backgroundSlider;
    public Slider narrationSlider;
    public Slider sfxSlider;

    private static SoundManager _instance;

    void Awake()
    {
        // Singleton / persistence
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplySavedSettings();
        HookUpSliders();
    }

    void HookUpSliders()
    {
        if (masterSlider != null) {
            masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (backgroundSlider != null) {
            backgroundSlider.onValueChanged.RemoveListener(SetBackgroundVolume);
            backgroundSlider.onValueChanged.AddListener(SetBackgroundVolume);
        }

        if (narrationSlider != null) {
            narrationSlider.onValueChanged.RemoveListener(SetNarrationVolume);
            narrationSlider.onValueChanged.AddListener(SetNarrationVolume);
        }

        if (sfxSlider != null) {
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    void ApplySavedSettings()
    {
        var s = SettingsManager.Instance.CurrentSettings;

        // Apply to mixer (these methods also save back to settings)
        SetMasterVolume(s.masterVolume);
        SetBackgroundVolume(s.backgroundVolume);
        SetNarrationVolume(s.narrationVolume);
        SetSFXVolume(s.sfxVolume);

        // Update UI sliders if present
        if (masterSlider) masterSlider.value = s.masterVolume;
        if (backgroundSlider) backgroundSlider.value = s.backgroundVolume;
        if (narrationSlider) narrationSlider.value = s.narrationVolume;
        if (sfxSlider) sfxSlider.value = s.sfxVolume;
    }

    // ---------- Volume Setters ----------
    public void SetMasterVolume(float value)
    {
        float db = LinearToDb(value);
        if (!mainAudioMixer.SetFloat("MasterVolume", db)) {
            Debug.LogWarning("MasterVolume parameter not found on mainAudioMixer");
        } else {
            Debug.Log($"Set MasterVolume to {db} dB (linear {value})");
        }
        SettingsManager.Instance.CurrentSettings.masterVolume = value;
        SettingsManager.Instance.Save();
    }

    public void SetBackgroundVolume(float value)
    {
        float db = LinearToDb(value);
        if (!mainAudioMixer.SetFloat("BackgroundVolume", db)) {
            Debug.LogWarning("BackgroundVolume parameter not found on mainAudioMixer");
        } else {
            Debug.Log($"Set BackgroundVolume to {db} dB (linear {value})");
        }
        SettingsManager.Instance.CurrentSettings.backgroundVolume = value;
        SettingsManager.Instance.Save();
    }

    public void SetNarrationVolume(float value)
    {
        float db = LinearToDb(value);
        if (!mainAudioMixer.SetFloat("NarrationVolume", db)) {
            Debug.LogWarning("NarrationVolume parameter not found on mainAudioMixer");
        } else {
            Debug.Log($"Set NarrationVolume to {db} dB (linear {value})");
        }
        SettingsManager.Instance.CurrentSettings.narrationVolume = value;
        SettingsManager.Instance.Save();
    }

    public void SetSFXVolume(float value)
    {
        float db = LinearToDb(value);
        if (!mainAudioMixer.SetFloat("SFXVolume", db)) {
            Debug.LogWarning("SFXVolume parameter not found on mainAudioMixer");
        } else {
            Debug.Log($"Set SFXVolume to {db} dB (linear {value})");
        }
        SettingsManager.Instance.CurrentSettings.sfxVolume = value;
        SettingsManager.Instance.Save();
    }

    // ---------- Helpers ----------
    float LinearToDb(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        return Mathf.Log10(value) * 20f;
    }
}
