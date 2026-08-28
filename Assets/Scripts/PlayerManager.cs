using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class PlayerManager : MonoBehaviour
{
    [Header("Movement Providers")]
    [SerializeField] private TeleportationProvider teleportationProvider;
    [SerializeField] private TeleportationActivator teleportationActivator;
    [SerializeField] private ContinuousMoveProvider continuousMoveProvider;
    //[SerializeField] private GameObject rightRayTeleport;

    [Header("Turning Providers")]
    [SerializeField] private SnapTurnProvider snapTurnProvider;
    [SerializeField] private ContinuousTurnProvider continuousTurnProvider;

    [Header("Other Player Settings")]
    [SerializeField] private GameObject vignette;
    [SerializeField] private GameObject captions;


    private void OnEnable()
    {
        // Listen for settings being loaded, saved, or reapplied.
        SettingsManager.OnSettingsLoaded += ApplySettings;
    }


    private void OnDisable()
    {
        // Always unsubscribe when this object is disabled.
        SettingsManager.OnSettingsLoaded -= ApplySettings;
    }


    private void Start()
    {
        // Apply the currently saved settings when the player first starts.
        //
        // This is useful even though we also listen to OnSettingsLoaded,
        // because the SettingsManager may have already loaded its settings
        // before this PlayerManager became active.

        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning(
                "PlayerManager: SettingsManager.Instance was not found."
            );

            return;
        }

        if (SettingsManager.Instance.settingsData == null)
        {
            Debug.LogWarning(
                "PlayerManager: SettingsData has not been loaded."
            );

            return;
        }

        ApplySettings(SettingsManager.Instance.settingsData);
    }


    /// <summary>
    /// Applies all player-related settings from SettingsData.
    /// </summary>
    private void ApplySettings(SettingsData data)
    {
        if (data == null)
        {
            Debug.LogWarning(
                "PlayerManager: Cannot apply settings because SettingsData is null."
            );

            return;
        }


        // Movement
        ApplyMovementMode(data.movementMode);


        // Turning
        ApplyTurningMode(data.turningMode);


        // Comfort vignette
        if (vignette != null)
        {
            vignette.SetActive(data.comfortVignette);
        }


    }


    /// <summary>
    /// Applies the saved movement mode.
    ///
    /// "Teleport"   = Teleportation Provider ON
    ///                Continuous Move Provider OFF
    ///
    /// "Continuous" = Teleportation Provider OFF
    ///                Continuous Move Provider ON
    /// </summary>
    private void ApplyMovementMode(string mode)
    {
        bool useContinuousMovement = string.Equals(
            mode,
            "Continuous",
            StringComparison.OrdinalIgnoreCase
        );

        if (useContinuousMovement)
        {
            EnableContinuousMovement();
            DisableTeleportation();
            Debug.Log(
                "PlayerManager: Continuous movement enabled."
            );
        }
        else
        {
            EnableTeleportation();
            DisableContinuousMovement();
            Debug.Log(
                "PlayerManager: Teleportation movement enabled."
            );
        }
    }

    private void EnableTeleportation()
    {
        //rightRayTeleport.SetActive(true);
        teleportationProvider.enabled = true;
        teleportationActivator.enabled = true;
    }

    private void DisableTeleportation()
    {
        //rightRayTeleport.SetActive(false);
        teleportationProvider.enabled = false;
        teleportationActivator.enabled = false;
    }

    private void EnableContinuousMovement()
    {
        continuousMoveProvider.enabled = true;
    }

    private void DisableContinuousMovement()
    {
        continuousMoveProvider.enabled = false;
    }


    /// <summary>
    /// Applies the saved turning mode.
    ///
    /// "Snap"   = Snap Turn Provider ON
    ///            Continuous Turn Provider OFF
    ///
    /// "Smooth" = Snap Turn Provider OFF
    ///            Continuous Turn Provider ON
    /// </summary>
    private void ApplyTurningMode(string mode)
    {
        bool useSmoothTurning = string.Equals(
            mode,
            "Smooth",
            StringComparison.OrdinalIgnoreCase
        );

    }
}