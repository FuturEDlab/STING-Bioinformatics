using UnityEngine;

/// <summary>
/// Brings up the XR Interaction Simulator when the game runs without a headset, so the scene
/// is playable with mouse and keyboard, and stays out of the way when it runs with one.
///
/// The simulator feeds Unity's Input System a virtual HMD and a virtual controller per hand.
/// Everything downstream — the rig's tracked pose drivers, XRI's interactors, and this
/// project's <see cref="XRInputRouter"/> — reads those exactly as it reads real hardware, so
/// desktop play exercises the same code path the headset will. That is the reason for
/// spawning the simulator rather than writing a separate mouse-picking mode: there is only
/// ever one input path to keep working.
///
/// It is spawned rather than left sitting in the scene because the simulator does not stand
/// down on its own — on device it would publish a second set of phantom controllers
/// alongside the real ones.
/// </summary>
[DefaultExecutionOrder(-200)]
public class DesktopSimulatorSpawner : MonoBehaviour
{
    [Tooltip("The XR Interaction Simulator prefab. Ships with the XR Interaction Toolkit samples: Assets/Samples/XR Interaction Toolkit/<version>/XR Interaction Simulator/XR Interaction Simulator.prefab")]
    [SerializeField] private GameObject simulatorPrefab;

    [Tooltip("Spawn it even when a headset is running. Only useful when debugging the simulator itself.")]
    [SerializeField] private bool alsoSpawnInHeadset;

    [Tooltip("Log a line saying which mode the scene came up in. Worth leaving on — 'why is nothing tracking?' is usually answered by it.")]
    [SerializeField] private bool logMode = true;

    /// <summary>True when a real XR display is running, so the simulator should stay away.</summary>
    public static bool HeadsetRunning => UnityEngine.XR.XRSettings.isDeviceActive;

    private void Awake()
    {
        if (HeadsetRunning && !alsoSpawnInHeadset)
        {
            if (logMode)
                Debug.Log($"[DesktopSimulatorSpawner] Headset detected ({UnityEngine.XR.XRSettings.loadedDeviceName}) — running on real tracking.", this);
            return;
        }

        if (simulatorPrefab == null)
        {
            Debug.LogWarning("[DesktopSimulatorSpawner] No simulator prefab assigned, so there is no mouse/keyboard control of the rig. Assign 'XR Interaction Simulator.prefab' from the XR Interaction Toolkit samples.", this);
            return;
        }

        GameObject simulator = Instantiate(simulatorPrefab);
        simulator.name = simulatorPrefab.name;

        if (logMode)
            Debug.Log("[DesktopSimulatorSpawner] No headset — XR Interaction Simulator spawned. Hold right mouse to look, WASD to walk, [ and ] to take hold of the left/right hand, G to grab, T for trigger, 1 and 2 for the face buttons. Press X in play mode for the full control sheet.", this);
    }
}
