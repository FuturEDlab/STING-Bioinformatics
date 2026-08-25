using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Makes props authored for the old BNG hands pickable by the new XRI hands, at runtime.
///
/// Most of this project's props are made grabbable by <see cref="PickUpGroup"/>, which
/// historically stamped a BNG <c>Grabbable</c> on every child. A scene running the new
/// <see cref="PlayerRig"/> has no BNG grabber to answer those, so without this the bottles,
/// the scanner and the syringe would sit there and refuse to be picked up.
///
/// Drop this on any object in an XRI scene — the rig itself is the obvious home. On
/// <c>Awake</c> it walks the loaded scene, and for every BNG <c>Grabbable</c> it finds:
/// switches it off, strips the remote-grab ring helper (the blue/orange circles, which are
/// deliberately not being ported), and adds an <see cref="XRGrabInteractable"/> tuned to
/// feel like the BNG one did.
///
/// Scenes whose props already carry <see cref="XRGrabInteractable"/> — which is how
/// <c>Mohamed Test Scene</c> is authored — pass straight through it and nothing is
/// converted. It exists so that dragging an older prefab into the scene later still works.
///
/// It does nothing at all in a scene with no <see cref="PlayerRig"/>, so it is safe to leave
/// on a prefab shared with the BNG scene.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-500)]
public class XRGrabBridge : MonoBehaviour
{
    [Header("Grab feel — these mirror what the BNG Grabbable was set to")]
    [Tooltip("Velocity Tracking reproduces the old physics-joint feel: the object chases the hand through the physics engine, so it bumps into the table instead of passing through it. Kinematic snaps it to the hand and ignores collisions; Instantaneous is the same but without smoothing.")]
    [SerializeField] private XRBaseInteractable.MovementType movementType = XRBaseInteractable.MovementType.VelocityTracking;

    [Tooltip("Grab the object where the hand actually touched it, instead of snapping it to one fixed spot. Leave this on — snapping makes a bottle jump into the palm at whatever angle it was authored at.")]
    [SerializeField] private bool useDynamicAttach = true;

    [Tooltip("Let go with speed, i.e. throwing. Off by default: nothing in this simulation is meant to be thrown, and tracked hands report a speed spike on the frame a grab ends, which is exactly what sends a gently released bottle across the room.")]
    [SerializeField] private bool throwOnDetach;

    [Tooltip("Which interaction layers converted props answer on. Must overlap the interactors on the hands — the rig's direct interactors use layer 1 (Default).")]
    [SerializeField] private InteractionLayerMask interactionLayers = 1;

    [Tooltip("Log one line per converted prop. Worth leaving on while setting a scene up — it is the quickest answer to 'why can't I pick that up?'.")]
    [SerializeField] private bool logConversions = true;

    /// <summary>Props converted on this scene's startup. Read for diagnostics.</summary>
    public IReadOnlyList<GameObject> Converted => converted;

    private readonly List<GameObject> converted = new List<GameObject>();

    private void Awake()
    {
        // A BNG scene must be left exactly as it was — that scene still has real BNG hands
        // and converting its props would break them.
        if (!Rig.UsingXRHands)
            return;

        BNG.Grabbable[] grabbables = FindObjectsByType<BNG.Grabbable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < grabbables.Length; i++)
            Convert(grabbables[i]);

        if (logConversions && converted.Count > 0)
        {
            Debug.Log($"[XRGrabBridge] Converted {converted.Count} BNG grabbable(s) to XRGrabInteractable so the new hands can pick them up: " +
                      string.Join(", ", converted.ConvertAll(g => g.name)), this);
        }
    }

    /// <summary>
    /// Clear up the stray "InputBridge" object BNG leaves behind.
    ///
    /// <c>Grabbable.Awake</c> reads <c>InputBridge.Instance</c>, and that getter *creates* an
    /// InputBridge GameObject when it cannot find one. Awake runs on disabled components too,
    /// so switching the Grabbables off above does not prevent it — by the time this runs there
    /// is an orphan sitting at the top of the hierarchy with no rig under it, doing nothing but
    /// confusing whoever opens the scene next.
    ///
    /// Only ever removes one that was clearly auto-created: no player controller under it, no
    /// children, and nothing else on the object.
    /// </summary>
    private void Start()
    {
        if (!Rig.UsingXRHands)
            return;

        BNG.InputBridge[] bridges = FindObjectsByType<BNG.InputBridge>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < bridges.Length; i++)
        {
            BNG.InputBridge bridge = bridges[i];
            if (bridge == null)
                continue;

            bool isOrphan = bridge.GetComponentInChildren<BNG.BNGPlayerController>(true) == null
                            && bridge.transform.childCount == 0
                            && bridge.GetComponents<Component>().Length == 2;   // Transform + InputBridge

            if (!isOrphan)
                continue;

            if (logConversions)
                Debug.Log("[XRGrabBridge] Removed the empty InputBridge object BNG spawns when a Grabbable wakes up without a BNG rig. Nothing in this scene reads it.", this);

            Destroy(bridge.gameObject);
        }
    }

    private void Convert(BNG.Grabbable grabbable)
    {
        if (grabbable == null)
            return;

        GameObject target = grabbable.gameObject;

        // The remote-grab ring visuals are BNG-only and are not being ported, so take them
        // out rather than leave a helper hunting for grabbers that do not exist.
        BNG.GrabbableRingHelper ring = target.GetComponent<BNG.GrabbableRingHelper>();
        if (ring != null)
            Destroy(ring);

        grabbable.enabled = false;

        // Already converted — by hand in the scene, or by a second bridge. Leave it alone;
        // re-adding would wipe whatever was tuned on it.
        if (target.GetComponent<XRGrabInteractable>() != null)
            return;

        // Velocity tracking drives the body through physics, so there has to be one — and it
        // has to go on before the interactable. XRGrabInteractable carries
        // [RequireComponent(Rigidbody)], so adding it first would have Unity supply a default
        // Rigidbody and these settings would never be applied.
        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = target.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.angularDamping = 0.5f;
        }

        XRGrabInteractable interactable = target.AddComponent<XRGrabInteractable>();
        interactable.movementType = movementType;
        interactable.useDynamicAttach = useDynamicAttach;
        interactable.throwOnDetach = throwOnDetach;
        interactable.interactionLayers = interactionLayers;

        // Single is the equivalent of BNG's SwapHands: taking it with the other hand hands
        // it over rather than tearing it between the two.
        interactable.selectMode = InteractableSelectMode.Single;

        converted.Add(target);
    }
}
