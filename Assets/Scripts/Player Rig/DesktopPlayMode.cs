using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// <summary>
/// Decides how the scene is driven when it comes up: a real headset, a mouse, or the XR
/// Interaction Simulator. One component, no per-platform scenes, nothing to switch by hand
/// before a build.
///
/// <b>Headset plugged in</b> — this stands down completely. The rig's tracked pose drivers
/// and XRI's interactors take over and the scene plays on device exactly as authored. That is
/// the whole point of leaving this in the scene: pushing to a Quest needs no edit.
///
/// <b>No headset</b> — one of two desktop modes comes up instead:
///
///  * <see cref="DesktopMode.MousePointer"/> (default) — point at something and click it.
///    Fastest way to walk the scenario at a desk. See <see cref="DesktopMousePointer"/>.
///  * <see cref="DesktopMode.XRSimulator"/> — Unity's XR Interaction Simulator, which feeds
///    the Input System a virtual headset and a virtual controller per hand. Slower to drive,
///    but it exercises the exact code path the headset will, so it is what to use when the
///    question is "will this work on device?".
///
/// Both read the same scene. Neither is a separate mode the game has to be built for.
///
/// <b>Why the decision is not made in a single read.</b> <see cref="XRSettings.isDeviceActive"/>
/// is only true once the XR display is actually running. On a Quest build XR Management has its
/// loader up before the first scene loads, but the OpenXR session needs a few more frames to
/// reach a running state — so on device, on frame one, with a headset on the player's head,
/// that property is routinely still false. Answering "is there a headset?" from that one read
/// is what put the mouse pointer on the rig on device, and the pointer switches the head's
/// tracked pose driver off: the camera then stayed nailed to the camera offset, which in Floor
/// tracking mode sits at 0. Eyes on the ground, hands parked in front of them. So the question
/// is now asked differently: if an XR loader is running, wait for the display rather than guess.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-200)]
public class DesktopPlayMode : MonoBehaviour
{
    public enum DesktopMode
    {
        [InspectorName("Mouse pointer — click things directly (recommended)")] MousePointer,
        [InspectorName("XR Interaction Simulator — virtual controllers")] XRSimulator,
        [InspectorName("Nothing — scene is inert without a headset")] Off,
    }

    [Header("Without a headset")]
    [Tooltip("How the scene is driven at a desk. Ignored when a headset is running.")]
    [SerializeField] private DesktopMode desktopMode = DesktopMode.MousePointer;

    [Tooltip("Only needed for XR Simulator mode. Ships with the toolkit at Assets/Samples/XR Interaction Toolkit/<version>/XR Interaction Simulator/XR Interaction Simulator.prefab.")]
    [SerializeField] private GameObject simulatorPrefab;

    [Header("Headset detection")]
    [Tooltip("Seconds to wait for the XR display to start before giving up and coming up in a desktop mode. Only ever waited when an XR loader is actually running, so at a desk with no XR runtime there is no delay at all. On a headset this is the window the session has to reach a running state — a couple of frames in practice.")]
    [SerializeField] private float headsetGraceSeconds = 5f;

    [Header("Overrides")]
    [Tooltip("Run the desktop mode even when a headset is connected. Only useful when debugging this component itself — on device it publishes a second, phantom set of controllers and takes head tracking away.")]
    [SerializeField] private bool alsoRunInHeadset;

    [Tooltip("Log one line saying which mode the scene came up in. Worth leaving on: 'why is nothing tracking?' is almost always answered by it.")]
    [SerializeField] private bool logMode = true;

    /// <summary>True when a real XR display is running, so the desktop helpers should stay away.</summary>
    public static bool HeadsetRunning => XRSettings.isDeviceActive;

    /// <summary>
    /// True when XR Management has a loader up. On a headset build this is already true on the
    /// first frame — well before <see cref="HeadsetRunning"/> is — which is what makes it worth
    /// waiting on. At a desk with no XR runtime it stays false and nothing is waited for.
    /// </summary>
    public static bool HeadsetStarting
    {
        get
        {
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            XRManagerSettings manager = settings != null ? settings.Manager : null;
            return manager != null && manager.activeLoader != null;
        }
    }

    /// <summary>Which mode actually came up. Off when the scene is running on a headset.</summary>
    public DesktopMode ActiveMode { get; private set; } = DesktopMode.Off;

    private DesktopMousePointer pointer;
    private GameObject spawnedSimulator;

    private void Awake()
    {
        pointer = GetComponent<DesktopMousePointer>();

        // Held off until the decision is made. This component runs at -200, ahead of the
        // pointer's own OnEnable, and that ordering is load-bearing: the pointer switches head
        // tracking off the moment it comes up, so it must not come up before we know whether a
        // headset is on its way.
        if (pointer != null)
            pointer.enabled = false;

        StartCoroutine(Decide());
    }

    private IEnumerator Decide()
    {
        // One frame either way, headset or not. A desktop mode reads the rig through Rig/PlayerRig
        // the moment it comes up, and PlayerRig resolves itself in its own Awake — which runs
        // after this one. Waiting a frame is what keeps the pointer from picking up a half-filled
        // rig and quietly falling back to Camera.main with no hands and no capsule.
        yield return null;

        if (alsoRunInHeadset)
        {
            StartDesktopMode();
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + Mathf.Max(0f, headsetGraceSeconds);

        // No loader means no XR runtime at all — that is the desk, and this loop does not run.
        // A loader that is up but whose display has not started yet is a headset mid-launch,
        // and it is worth the handful of frames.
        while (!HeadsetRunning && HeadsetStarting && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (HeadsetRunning)
        {
            StandDown($"Headset detected ({XRSettings.loadedDeviceName})");
            yield break;
        }

        if (HeadsetStarting)
        {
            Debug.LogWarning($"[DesktopPlayMode] An XR loader is running but no XR display started within {headsetGraceSeconds:0.#}s, so the scene is coming up in a desktop mode. On a headset that means no head tracking — raise Headset Grace Seconds if the device is simply slow to start.", this);
        }

        StartDesktopMode();

        // A headset can still arrive after the scene is up — XR started late, or the display
        // was reconnected. Standing down then is what stops a late arrival being left with head
        // tracking switched off.
        StartCoroutine(WatchForLateHeadset());
    }

    private IEnumerator WatchForLateHeadset()
    {
        while (!HeadsetRunning)
            yield return null;

        StandDown($"Headset detected ({XRSettings.loadedDeviceName}) after the scene came up");
    }

    private void StartDesktopMode()
    {
        ActiveMode = desktopMode;

        switch (desktopMode)
        {
            case DesktopMode.MousePointer:
                if (pointer == null)
                    pointer = gameObject.AddComponent<DesktopMousePointer>();
                pointer.enabled = true;

                if (logMode)
                    Debug.Log("[DesktopPlayMode] No headset — mouse pointer mode. Point at something and left-click to use it; click a prop to pick it up and again to put it down. Hold right mouse to look, WASD to walk, scroll to push a held object away.", this);
                break;

            case DesktopMode.XRSimulator:
                SpawnSimulator();
                break;

            case DesktopMode.Off:
                if (logMode)
                    Debug.Log("[DesktopPlayMode] No headset and desktop mode is Off — nothing will respond to input.", this);
                break;
        }
    }

    /// <summary>
    /// Hand the rig back to its tracked pose drivers and XRI's interactors, undoing anything a
    /// desktop mode had already done to it.
    /// </summary>
    private void StandDown(string reason)
    {
        ActiveMode = DesktopMode.Off;

        if (pointer != null)
            pointer.enabled = false;

        if (spawnedSimulator != null)
        {
            Destroy(spawnedSimulator);
            spawnedSimulator = null;
        }

        RestoreHeadTracking();

        if (logMode)
            Debug.Log($"[DesktopPlayMode] {reason} — real tracking, desktop helpers off.", this);
    }

    /// <summary>
    /// Put the head's tracked pose driver back. <see cref="DesktopMousePointer"/> switches it
    /// off so it cannot fight the mouse for the camera's rotation, and its OnDisable restores
    /// it — this covers the rest: a driver left disabled for any other reason would pin the
    /// camera to the camera offset, which in Floor tracking mode sits on the floor.
    /// </summary>
    private void RestoreHeadTracking()
    {
        Camera head = GetComponentInChildren<Camera>(true);
        if (head == null)
            return;

        UnityEngine.InputSystem.XR.TrackedPoseDriver driver =
            head.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();

        if (driver != null && !driver.enabled)
            driver.enabled = true;
    }

    private void SpawnSimulator()
    {
        if (simulatorPrefab == null)
        {
            Debug.LogWarning("[DesktopPlayMode] XR Simulator mode is selected but no simulator prefab is assigned, so nothing will drive the rig. Assign 'XR Interaction Simulator.prefab' from Assets/Samples/XR Interaction Toolkit/, or switch this component to Mouse Pointer.", this);
            return;
        }

        spawnedSimulator = Instantiate(simulatorPrefab);
        spawnedSimulator.name = simulatorPrefab.name;

        if (logMode)
            Debug.Log("[DesktopPlayMode] No headset — XR Interaction Simulator spawned. Hold right mouse to look, WASD to walk, [ and ] to take hold of the left/right hand, G to grip, T for trigger. Press X in play mode for the full control sheet.", this);
    }
}
