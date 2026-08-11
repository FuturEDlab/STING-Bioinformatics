using System;
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
    [SerializeField] private Behaviour snapTurnProvider;
    [SerializeField] private Behaviour continuousTurnProvider;
    [SerializeField] private GameObject vignetteObject;
    [SerializeField] private GameObject captionsObject;

    private SettingsManager settingsManager = null;

    void OnEnable()
    {
        SettingsManager.OnSettingsLoaded += ApplySettings;
    }

    void OnDisable()
    {
        SettingsManager.OnSettingsLoaded -= ApplySettings;
    }

    void Start()
    {
        settingsManager = SettingsManager.Instance;
        if (settingsManager != null)
        {
            ApplySettings(settingsManager.settingsData);
        }
    }

    public void ApplySettings(SettingsData data)
    {
        if (data == null) return;

        ApplySavedMovementMode(data.movementMode);
        ApplyTurningMode(data.turningMode);
        SetVignetteActive(data.comfortVignette);
        SetCaptionsActive(data.subtitles);
    }

    public void ApplySavedMovementMode(string mode)
    {
        if (string.IsNullOrEmpty(mode))
        {
            if (settingsManager == null || settingsManager.settingsData == null) return;
            mode = settingsManager.settingsData.movementMode;
        }

        if (string.Equals(mode, "Teleport", StringComparison.InvariantCultureIgnoreCase))
        {
            SetTeleportationActive(true);
        }
        else if (string.Equals(mode, "Continuous", StringComparison.InvariantCultureIgnoreCase))
        {
            SetTeleportationActive(false);
        }
        else
        {
            Debug.LogWarning("Unknown movement mode: " + mode);
        }
    }

    private void SetTeleportationActive(bool enabled)
    {
        if (teleportPlayer != null) teleportPlayer.enabled = enabled;

        bool useContinuous = !enabled;
        if (smoothLoco != null) smoothLoco.enabled = useContinuous;
        if (locoMotion != null) locoMotion.SetActive(useContinuous);
        if (locoManager != null) locoManager.enabled = useContinuous;
    }

    public void ApplyTurningMode(string mode)
    {
        if (string.IsNullOrEmpty(mode))
        {
            if (settingsManager == null || settingsManager.settingsData == null) return;
            mode = settingsManager.settingsData.turningMode;
        }

        if (string.Equals(mode, "Snap", StringComparison.InvariantCultureIgnoreCase))
        {
            SetTurnProviderActive(snapTurnProvider, true);
            SetTurnProviderActive(continuousTurnProvider, false);
        }
        else if (string.Equals(mode, "Smooth", StringComparison.InvariantCultureIgnoreCase))
        {
            SetTurnProviderActive(snapTurnProvider, false);
            SetTurnProviderActive(continuousTurnProvider, true);
        }
        else
        {
            Debug.LogWarning("Unknown turning mode: " + mode);
        }
    }

    private void SetTurnProviderActive(Behaviour provider, bool enabled)
    {
        if (provider != null) provider.enabled = enabled;
    }

    public void SetVignetteActive(bool enabled)
    {
        if (vignetteObject != null) vignetteObject.SetActive(enabled);
    }

    public void SetCaptionsActive(bool enabled)
    {
        if (captionsObject != null) captionsObject.SetActive(enabled);
    }
}
