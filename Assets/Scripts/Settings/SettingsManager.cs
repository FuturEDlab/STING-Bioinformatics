using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class SettingsManager : MonoBehaviour
{
    private static string path = Application.persistentDataPath + "/settings.json";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Usually when passing in a variable it makes a copy of what you are referencing.
    //Ref makes it so when passing in a variable it uses the reference and this makes 
    //it so we can read and write data.

    public static void SaveSettingsData(SettingsData data)
    {
        data.narrationVolume = 1;
        data.movementMode = "Continous";
        data.turningMode = "Snap";
        data.subtitles = false;
        data.textSize = "Medium";
        data.comfortVignette = false;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public static SettingsData LoadSettingsData(SettingsData data)
    {
        //ex: transform.position = data.Position;
        if (!File.Exists(path))
        {
            Debug.Log("No json file found in " + path);
            return null;
        }

        string json = File.ReadAllText(path);
        SettingsData loadedData = JsonUtility.FromJson<SettingsData>(json);
        Debug.Log(loadedData);
        return loadedData;
    }
}
