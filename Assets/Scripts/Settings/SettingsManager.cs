using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Drawing.Text;
using Unity.VisualScripting;


public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance {get; private set;}
    public SettingsData settingsData {get; private set;}
    private SettingsData pendingSettingsData;
    private static string path;
    public Slider subtitlesToggle;
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
        
        settingsData = LoadSettingsData();

        if (settingsData == null)
        {
            settingsData = new SettingsData();
        }

        //create a copy of the settings data to hold pending changes
        pendingSettingsData = CloneSettingsData(settingsData);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ManageSubtitles()
    {
        pendingSettingsData.subtitles = subtitlesToggle.value > 0.5f;
        Debug.Log("Pending subtitles setting: " + pendingSettingsData.subtitles);

        /*if (subtitlesToggle.value < 0.5)
        {
            //Todo: create a copy of the settings and then update the
            //copy with changed setting.
            //If the save button is pressed while still in settings, save the copy
            //otherwise if close is pressed, clear copy of settings and
            //do not save
            areSubtitlesActive = false;
            Debug.Log("Subtitles Off");
        }
        else if (subtitlesToggle.value > 0.5)
        {
            //Todo: same as above
            areSubtitlesActive = true;
            Debug.Log("Subtitles On");
        }*/
    }

    public void OnSaveButtonClicked()
    {
        ApplyPendingSettings();
        SaveSettingsData();
    }

    public void OnCancelButtonClicked()
    {
        pendingSettingsData = CloneSettingsData(settingsData);
        Debug.Log("Pending settings data has been reset to current settings data");
    }

    public void ApplyPendingSettings()
    {
        settingsData = CloneSettingsData(pendingSettingsData);
        Debug.Log("Pending settings data has been applied to current settings data");
    }

    private SettingsData CloneSettingsData(SettingsData originalData)
    {
        if (originalData == null)
        {
            return new SettingsData();
        }

        SettingsData clone = new SettingsData
        {
            narrationVolume = originalData.narrationVolume,
            movementMode = originalData.movementMode,
            turningMode = originalData.turningMode,
            subtitles = originalData.subtitles,
            textSize = originalData.textSize,
            comfortVignette = originalData.comfortVignette
        };

        return clone;
    }

    private void SaveSettingsData()
    {
        //Filler data for testing
        /*settingsData.narrationVolume = 2.0f;
        settingsData.movementMode = "Continous";
        settingsData.turningMode = "Snap";
        settingsData.subtitles = false;
        settingsData.textSize = "Medium";
        settingsData.comfortVignette = false;*/

        string json = JsonUtility.ToJson(settingsData, true);
        File.WriteAllText(path, json);

        Debug.Log("Settings data has been saved");
        Debug.Log(settingsData.narrationVolume + " ," + settingsData.movementMode + " ," + settingsData.turningMode + " ," + settingsData.subtitles + " ," + settingsData.textSize + " ," + settingsData.comfortVignette);

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
        Debug.Log(loadedData.narrationVolume + ", " + loadedData.movementMode + ", " + loadedData.turningMode + ", " + loadedData.subtitles + ", " + loadedData.textSize + ", " + loadedData.comfortVignette);
        return loadedData;
    }
}
