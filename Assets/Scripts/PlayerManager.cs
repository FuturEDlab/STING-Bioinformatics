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

    private SettingsManager sInstance;
    private SettingsData settings;
    
    void Start()
    {
        sInstance = SettingsManager.Instance;
        if (sInstance == null) return;
        settings = SettingsManager.Instance.CurrentSettings;
        if (settings == null) return;
        
        WireLocomotion(settings.locomotionEnabled);
        WireSnapTurn(settings.snapTurningEnabled);
        WireTeleport(settings.teleportationEnabled);
    }

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
        bool smoothLocoOn = settings.locomotionEnabled;
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

    }
}
