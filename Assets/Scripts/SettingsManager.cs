using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public SettingsData CurrentSettings;

    private SettingsData backupSettings;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    void LoadSettings()
    {
        CurrentSettings = SettingsSaveSystem.Load();

        if (CurrentSettings == null)
        {
            CurrentSettings = new SettingsData();
            SettingsSaveSystem.Save(CurrentSettings);
        }
    }

    public void BeginTemporaryEdit()
    {
        backupSettings = Copy(CurrentSettings);
    }

    public void CancelTemporaryEdit()
    {
        if (backupSettings == null) return;

        CopyInto(backupSettings, CurrentSettings);
    }

    public void Save()
    {
        SettingsSaveSystem.Save(CurrentSettings);
    }

    SettingsData Copy(SettingsData source)
    {
        return new SettingsData
        {
            locomotionEnabled = source.locomotionEnabled,
            snapTurningEnabled = source.snapTurningEnabled,
            teleportationEnabled = source.teleportationEnabled,
            vignettingEnabled = source.vignettingEnabled,
            subtitlesEnabled = source.subtitlesEnabled,

            masterVolume = source.masterVolume,
            backgroundVolume = source.backgroundVolume,
            narrationVolume = source.narrationVolume,
            sfxVolume = source.sfxVolume
        };
    }

    void CopyInto(SettingsData source, SettingsData target)
    {
        target.locomotionEnabled = source.locomotionEnabled;
        target.snapTurningEnabled = source.snapTurningEnabled;
        target.teleportationEnabled = source.teleportationEnabled;
        target.vignettingEnabled = source.vignettingEnabled;
        target.subtitlesEnabled = source.subtitlesEnabled;

        target.masterVolume = source.masterVolume;
        target.backgroundVolume = source.backgroundVolume;
        target.narrationVolume = source.narrationVolume;
        target.sfxVolume = source.sfxVolume;
    }
}