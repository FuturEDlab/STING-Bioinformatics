using UnityEngine;
using BNG;

public class PlayerManager : MonoBehaviour
{
    /*
    Look at SampleSceneV6 scene to see how this script can be used
    and doing so will also show what should be inserted into each
    serialized field when in the Unity Editor.
    */
    
    [SerializeField] private LocomotionManager locoManager;
    [SerializeField] private GameObject locoMotion;
    [SerializeField] private PlayerTeleport teleportPlayer;
    [SerializeField] private SmoothLocomotion smoothLoco;
    [SerializeField] private PlayerRotation playerRotate;
    private SettingsManager settingsManager = null;

    //private SettingsManager sInstance;
    private SettingsData settings;


    void Start()
    {
        // Resolve the settings manager at Start (safer for script execution order)
        settingsManager = SettingsManager.Instance;

        // Apply saved movement mode when the scene starts
        ApplySavedMovementMode(settingsManager.settingsData.movementMode);
    }

    public void ApplySavedMovementMode(string mode)
    {
        // If caller didn't pass a mode, read saved settings
        if (string.IsNullOrEmpty(mode))
        {
            if (settingsManager == null || settingsManager.settingsData == null) return;
            mode = settingsManager.settingsData.movementMode;
        }

        // Expect exact values: "Teleport" or "Continuous"
        if (mode == "Teleport")
        {
            if (teleportPlayer != null) teleportPlayer.enabled = true;
            if (smoothLoco != null) smoothLoco.enabled = false;
            if (locoMotion != null) locoMotion.SetActive(false);
            if (locoManager != null) locoManager.enabled = false;
        }
        else if (mode == "Continuous")
        {
            if (teleportPlayer != null) teleportPlayer.enabled = false;
            if (smoothLoco != null) smoothLoco.enabled = true;
            if (locoMotion != null) locoMotion.SetActive(true);
            if (locoManager != null) locoManager.enabled = true;
        }
        else
        {
            Debug.LogWarning("Unknown movement mode: " + mode);
        }
    }
    

    /*
    public void WireLocomotion(bool value)
    {
        bool teleportOn = settings.teleportationEnabled;
        bool isLocoManagerOn = false;
        
        if (smoothLoco != null)
        {
            smoothLoco.enabled = value;
        }

        if (teleportOn && smoothLoco.enabled)
        {
            isLocoManagerOn = true;
        }
        
        if (locoManager != null)
        {
            locoManager.enabled = isLocoManagerOn;
        }
        
    }
    
    public void WireTeleport(bool value)
    {
        //bool smoothLocoOn = settings.locomotionEnabled;
        bool isLocoManagerOn = false;
        
        if (teleportPlayer != null)
        {
            teleportPlayer.enabled = value;
        }
        
        if (locoMotion != null)
        {
            locoMotion.SetActive(value);
        }

        if (smoothLocoOn && teleportPlayer.enabled)
        {
            isLocoManagerOn = true;
        }
        
        if (locoManager != null)
        {
            locoManager.enabled = isLocoManagerOn;
        }
    }
    
    public void WireSnapTurn(bool value)
    {
        if (playerRotate != null)
        {
            playerRotate.enabled = value;
        }
    }
    
    public void WireVignetting(bool value)
    {

    }
    
    public void WireSubtitles(bool value)
    {

    }*/
}
