using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public SettingsData CurrentSettings { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentSettings = SettingsSaveSystem.Load();
    }

    public void Save()
    {
        SettingsSaveSystem.Save(CurrentSettings);
    }
}