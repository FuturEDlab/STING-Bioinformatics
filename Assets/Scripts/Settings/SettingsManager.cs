using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class SettingsManager : MonoBehaviour
{
    private static string path;
    public static SettingsData settingsData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        path = Application.persistentDataPath + "/settings.json";
        settingsData = LoadSettingsData();

        if (settingsData == null)
        {
            settingsData = new SettingsData();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Usually when passing in a variable it makes a copy of what you are referencing.
    //Ref makes it so when passing in a variable it uses the reference and this makes 
    //it so we can read and write data.

    public void OnSaveButtonClicked()
    {
        SaveSettingsData();
    }

    public static void SaveSettingsData()
    {
        settingsData.narrationVolume = 2.0f;
        settingsData.movementMode = "Continous";
        settingsData.turningMode = "Snap";
        settingsData.subtitles = false;
        settingsData.textSize = "Medium";
        settingsData.comfortVignette = false;

        string json = JsonUtility.ToJson(settingsData, true);
        File.WriteAllText(path, json);

        Debug.Log("Settings data has been saved");
        Debug.Log(settingsData.narrationVolume + " ," + settingsData.movementMode + " ," + settingsData.turningMode + " ," + settingsData.subtitles + " ," + settingsData.textSize + " ," + settingsData.comfortVignette);

    }

    public static SettingsData LoadSettingsData()
    {
        //ex: transform.position = data.Position;
        if (!File.Exists(path))
        {
            Debug.Log("Data could not be loaded due to no json file found in " + path);
            return null;
        }

        string json = File.ReadAllText(path);
        SettingsData loadedData = JsonUtility.FromJson<SettingsData>(json);
        Debug.Log(loadedData.narrationVolume + " ," + loadedData.movementMode + " ," + loadedData.turningMode + " ," + loadedData.subtitles + " ," + loadedData.textSize + " ," + loadedData.comfortVignette);
        return loadedData;
    }
}
