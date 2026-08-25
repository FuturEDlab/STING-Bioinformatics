using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using System.IO;
using System.Drawing.Text;
using Unity.VisualScripting;


public class SettingsManager : MonoBehaviour
{
    public static event Action<SettingsData> OnSettingsLoaded;

    public static SettingsManager Instance {get; private set;}
    public SettingsData settingsData {get; private set;}
    private SettingsData pendingSettingsData;
    private static string path;
    [Header("Audio")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [FormerlySerializedAs("masterVolumeSlider")]
    [SerializeField] private Slider speechVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    public Slider subtitlesToggle;
    public Slider comfortVignetteSlider;
    private bool areSubtitlesActive; //Probably wont need this because pendingSettingsData.subtitles 
    //will be used to check if subtitles are active or not.

    void Awake()
    {
        //Settings manager is an instance and will be the way we reach the settings
        //data rather than making SettingsData an instance as SettingsData
        //is not a monobehavior and can not be attahced to a gameobject.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        path = Application.persistentDataPath + "/settings.json";

        Debug.Log($"SettingsManager Awake. settings.json path: {path}");

        settingsData = LoadSettingsData();

        if (settingsData == null)
        {
            settingsData = new SettingsData
            {
                speechVolume = 1f,
                sfxVolume = 1f,
                movementMode = "Teleport",
                turningMode = "Snap",
                subtitles = true,
                textSize = "Medium",
                comfortVignette = false
            };

            SaveSettingsData();
        }

        //create a copy of the settings data to hold pending changes
        pendingSettingsData = CloneSettingsData(settingsData);

        ApplyAudioVolumes(settingsData);

        //Apply loaded settings to the player when the game starts
        //TODO: load (ex: ApplySavedMovementMode) and apply settings to the player when the game starts
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OnSettingsLoaded?.Invoke(settingsData);
    }

    // NOTE: We do NOT automatically attach listeners. Use the inspector OnValueChanged / OnClick
    // to call the public setter methods below so pendingSettingsData is updated when controls change.

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSaveButtonClicked()
    {
        Debug.Log("SettingsManager: OnSaveButtonClicked invoked");
        ApplyPendingSettings();
        SaveSettingsData();
    }

    public void OnCancelButtonClicked()
    {
        pendingSettingsData = CloneSettingsData(settingsData);
        ApplyAudioVolumes(settingsData);
        RefreshAudioSliders(settingsData);
        Debug.Log("Pending settings data has been reset to current settings data");
    }

    public void ApplyPendingSettings()
    {
        settingsData = CloneSettingsData(pendingSettingsData);
        ApplyAudioVolumes(settingsData);
        Debug.Log("Pending settings data has been applied to current settings data");

        // Notify listeners (e.g., PlayerManager) that settings have been applied so runtime
        // systems can update themselves immediately.
        OnSettingsLoaded?.Invoke(settingsData);
    }

    // Public setter methods intended to be called from inspector OnValueChanged / OnClick
    public void SetSpeechVolume(float value)
    {
        pendingSettingsData.speechVolume = Mathf.Clamp01(value);
        ApplyAudioVolumes(pendingSettingsData);
    }

    public void SetSFXVolume(float value)
    {
        pendingSettingsData.sfxVolume = Mathf.Clamp01(value);
        ApplyAudioVolumes(pendingSettingsData);
    }

    public void SetMovementMode(int index)
    {
        var options = new[] { "Teleport", "Continuous" };
        if (index >= 0 && index < options.Length)
        {
            pendingSettingsData.movementMode = options[index];
            Debug.Log("Pending movementMode: " + pendingSettingsData.movementMode);
        }
    }

    public void SetTurningModeIndex(int index)
    {
        var options = new[] { "Snap", "Smooth" };
        if (index >= 0 && index < options.Length)
        {
            pendingSettingsData.turningMode = options[index];
            Debug.Log("Pending turningMode: " + pendingSettingsData.turningMode);
        }
    }

    // Subtitles: slider wiring in inspector
    public void SetSubtitlesFromSlider(float value)
    {
        pendingSettingsData.subtitles = value > 0.5f;
        Debug.Log("Pending subtitles (from slider): " + pendingSettingsData.subtitles);
    }

    // Subtitles: ToggleSwitch wiring in inspector using On Toggle On / On Toggle Off
    public void SetSubtitlesOn()
    {
        pendingSettingsData.subtitles = true;
        Debug.Log("Pending subtitles (toggle on): true");
    }

    public void SetSubtitlesOff()
    {
        pendingSettingsData.subtitles = false;
        Debug.Log("Pending subtitles (toggle off): false");
    }

    // Comfort vignette: slider wiring in inspector
    public void SetComfortVignetteFromSlider(float value)
    {
        pendingSettingsData.comfortVignette = value > 0.5f;
        Debug.Log("Pending comfortVignette (from slider): " + pendingSettingsData.comfortVignette);
    }

    // Comfort vignette: ToggleSwitch wiring in inspector using On Toggle On / On Toggle Off
    public void SetComfortVignetteOn()
    {
        pendingSettingsData.comfortVignette = true;
        Debug.Log("Pending comfortVignette (toggle on): true");
    }

    public void SetComfortVignetteOff()
    {
        pendingSettingsData.comfortVignette = false;
        Debug.Log("Pending comfortVignette (toggle off): false");
    }

    // Update old settings data with new settings data and save to json file
    private SettingsData CloneSettingsData(SettingsData originalData)
    {
        if (originalData == null)
        {
            return new SettingsData();
        }

        SettingsData clone = new SettingsData
        {
            speechVolume = originalData.speechVolume,
            sfxVolume = originalData.sfxVolume,
            movementMode = originalData.movementMode,
            turningMode = originalData.turningMode,
            subtitles = originalData.subtitles,
            textSize = originalData.textSize,
            comfortVignette = originalData.comfortVignette
        };

        return clone;
    }

    private void ApplyAudioVolumes(SettingsData data)
    {
        if (mainAudioMixer == null || data == null)
        {
            return;
        }

        SetMixerVolume("NarrationVolume", data.speechVolume);
        SetMixerVolume("SFXVolume", data.sfxVolume);
    }

    private void SetMixerVolume(string parameterName, float value)
    {
        float clampedValue = Mathf.Clamp01(value);
        float decibels = clampedValue <= 0f ? -80f : Mathf.Log10(clampedValue) * 20f;

        if (!mainAudioMixer.SetFloat(parameterName, decibels))
        {
            Debug.LogWarning($"SettingsManager: AudioMixer parameter '{parameterName}' was not found.");
        }
    }

    private void RefreshAudioSliders(SettingsData data)
    {
        if (data == null)
        {
            return;
        }

        if (speechVolumeSlider != null)
        {
            speechVolumeSlider.SetValueWithoutNotify(data.speechVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.SetValueWithoutNotify(data.sfxVolume);
        }
    }

    private void SaveSettingsData()
    {
        Debug.Log("Saving settings data to: " + path);
        // Persist the current settingsData to disk (assumed already applied via ApplyPendingSettings)
        try
        {
            string json = JsonUtility.ToJson(settingsData, true);
            File.WriteAllText(path, json);

            Debug.Log("Settings data has been saved to: " + path);
            Debug.Log("Saved settings JSON:\n" + json);
            Debug.Log($"Values -> speechVolume: {settingsData.speechVolume}, sfxVolume: {settingsData.sfxVolume}, movementMode: {settingsData.movementMode}, turningMode: {settingsData.turningMode}, subtitles: {settingsData.subtitles}, textSize: {settingsData.textSize}, comfortVignette: {settingsData.comfortVignette}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save settings data: " + ex.Message);
        }

    }

    public SettingsData LoadSettingsData()
    {
        //ex: transform.position = data.Position;
        if (!File.Exists(path))
        {
            Debug.Log("Data could not be loaded due to no json file found in " + path);
            return null;
        }

        string json = File.ReadAllText(path);
        SettingsData loadedData = JsonUtility.FromJson<SettingsData>(json);
        if (!json.Contains("\"speechVolume\"") && json.Contains("\"masterVolume\""))
        {
            LegacySettingsData legacyData = JsonUtility.FromJson<LegacySettingsData>(json);
            loadedData.speechVolume = legacyData.masterVolume;
        }
        Debug.Log(loadedData.speechVolume + ", " + loadedData.sfxVolume + ", " + loadedData.movementMode + ", " + loadedData.turningMode + ", " + loadedData.subtitles + ", " + loadedData.textSize + ", " + loadedData.comfortVignette);
        return loadedData;
    }

    [Serializable]
    private class LegacySettingsData
    {
        public float masterVolume;
    }
}
