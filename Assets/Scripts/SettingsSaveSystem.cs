using UnityEngine;
using System.IO;

public static class SettingsSaveSystem
{
    private static string path = Application.persistentDataPath + "/settings.json";

    public static void Save(SettingsData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public static SettingsData Load()
    {
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SettingsData>(json);
    }
}