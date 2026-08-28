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
    private const string SpeechVolumeParam = "SpeechVolume";
    private const string SFXVolumeParam = "SFXVolume";
    public Slider subtitlesToggle;
    public Slider comfortVignetteSlider;
    [SerializeField] private GameObject subtitles;
    [SerializeField] private GameObject comfortVignette;
    private bool areSubtitlesActive; //Probably wont need this because pendingSettingsData.subtitles 
    //will be used to check if subtitles are active or not.

    private void Awake()
    {
        // Set up the singleton.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        path = Application.persistentDataPath + "/settings.json";

        Debug.Log($"SettingsManager Awake. settings.json path: {path}");

        // Load saved settings.
        settingsData = LoadSettingsData();

        // If no settings file exists, create default settings.
        if (settingsData == null)
        {
            settingsData = new SettingsData
            {
                speechVolume = 1f,
                sfxVolume = 1f,
                movementMode = "Teleport",
                turningMode = "Snap",
                subtitles = true,
                comfortVignette = false
            };

            SaveSettingsData();
        }

        // Create an editable copy for the settings menu.
        pendingSettingsData = CloneSettingsData(settingsData);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ApplyCurrentSettings();
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
        // Throw away unsaved changes.
        pendingSettingsData = CloneSettingsData(settingsData);

        // Restore the currently saved settings to the game and UI.
        ApplyCurrentSettings();

        Debug.Log(
            "Pending settings have been discarded and current settings restored."
        );
    }

    private void ApplyCurrentSettings()
    {
        ApplyAllSettings(settingsData);

        // Tell UI and other listeners to refresh.
        OnSettingsLoaded?.Invoke(settingsData);
    }


    public void ApplyPendingSettings()
    {
        settingsData = CloneSettingsData(pendingSettingsData);

        Debug.Log(
            "Pending settings data has been applied to current settings data"
        );

        ApplyCurrentSettings();
    }

    private void ApplyAllSettings(SettingsData data)
    {
        if (data == null)
        {
            return;
        }

        ApplyAudioSettings(data);
        ApplyComfortVignette(data.comfortVignette);

        /*ApplyMovementMode(data.movementMode);
        ApplyTurningMode(data.turningMode);
        */
    }

    private void ApplyComfortVignette(bool enabled)
    {
        if (comfortVignette != null)
        {
            comfortVignette.SetActive(enabled);
        }
    }

    public void SetSpeechVolume(float value)
    {
        mainAudioMixer.SetFloat(SpeechVolumeParam, Mathf.Log10(value) * 20f);
        pendingSettingsData.speechVolume = value;
        Debug.Log($"Pending speechVolume set to {value}");
    }

    public void SetSFXVolume(float value)
    {
        mainAudioMixer.SetFloat(SFXVolumeParam, Mathf.Log10(value) * 20f);
        pendingSettingsData.sfxVolume = value;
        Debug.Log($"Pending sfxVolume set to {value}");
    }

    private void ApplyAudioSettings(SettingsData data)
    {
        if (data == null || mainAudioMixer == null)
        {
            Debug.Log("Cannot apply audio settings: data or mainAudioMixer is null.");
            return;
        }

        mainAudioMixer.SetFloat(
            SpeechVolumeParam,
            VolumeToDecibels(data.speechVolume)
        );

        mainAudioMixer.SetFloat(
            SFXVolumeParam,
            VolumeToDecibels(data.sfxVolume)
        );
    }

    private float VolumeToDecibels(float volume)
    {
        // Avoid Mathf.Log10(0), which would produce negative infinity.
        if (volume <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Log10(volume) * 20f;
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
            comfortVignette = originalData.comfortVignette
        };

        return clone;
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
            Debug.Log($"Values -> speechVolume: {settingsData.speechVolume}, sfxVolume: {settingsData.sfxVolume}, movementMode: {settingsData.movementMode}, turningMode: {settingsData.turningMode}, subtitles: {settingsData.subtitles}, comfortVignette: {settingsData.comfortVignette}");
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
        Debug.Log(loadedData.speechVolume + ", " + loadedData.sfxVolume + ", " + loadedData.movementMode + ", " + loadedData.turningMode + ", " + loadedData.subtitles + ", " + loadedData.comfortVignette);
        return loadedData;
    }

    [Serializable]
    private class LegacySettingsData
    {
        public float masterVolume;
    }
}
