using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;


public class PlayerManager : MonoBehaviour
{
    /*
    Look at SampleSceneV6 scene to see how this script can be used
    and doing so will also show what should be inserted into each
    serialized field when in the Unity Editor.
    */
    
    [SerializeField] private TeleportationProvider teleportationProvider;
    [SerializeField] private ContinuousMoveProvider continuousMoveProvider;
    [SerializeField] private ContinuousTurnProvider continuousTurnProvider;
    [SerializeField] private SnapTurnProvider snapTurnProvider;
    [SerializeField] private GameObject vignette;
    [SerializeField] private GameObject captions;

    [SerializeField] private AudioSource MasterAudioSource;

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
        
    }
}
