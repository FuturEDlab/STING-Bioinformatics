using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Play the scene with a mouse. Point at something, click it, and it does what it would do
/// if a tracked hand had touched it.
///
/// This exists because putting a headset on to check "does the scenario still advance past
/// the wristband?" is a slow way to work, and because the XR Interaction Simulator — which is
/// the other desktop option, see <see cref="DesktopPlayMode"/> — asks you to drive two virtual
/// controllers with the keyboard before you can touch anything.
///
/// <b>Controls</b>
/// <list type="bullet">
///   <item>Left click — use what is under the cursor. On a prop that means picking it up.</item>
///   <item>Left click while holding something — passes through to the held item, so the scanner
///         still fires on a click. It does not drop it and does not swap to another prop.</item>
///   <item><see cref="dropKey"/> (Q) — put the held item down.</item>
///   <item>Scroll — push the held item away or pull it closer.</item>
///   <item>Hold right mouse — look around.</item>
///   <item>WASD — walk. Shift to move faster.</item>
/// </list>
///
/// Nothing here is a parallel implementation of the game's rules. A click ends up calling the
/// same <see cref="ScenarioTarget.Activate"/> and <c>Interact.onInteract</c> the hands call, and
/// a carried prop reports itself as held through <see cref="GrabHandle"/>, so the scanner's
/// "must be held" gate, tasks that complete on pickup, and <see cref="GrabStability"/>'s
/// settle-down all behave exactly as they do on device.
///
/// <see cref="DesktopPlayMode"/> switches this off when a headset is running.
/// </summary>
[DisallowMultipleComponent]
public class DesktopMousePointer : MonoBehaviour
{
    // Props being carried by the mouse, so GrabHandle can report them as held without this
    // component having to be reachable from every prop in the room.
    private static readonly HashSet<GameObject> Carried = new HashSet<GameObject>();

    /// <summary>
    /// True while the mouse has hold of this object, or of anything it hangs off. The walk
    /// up the hierarchy matters because the grab component and the collider that was clicked
    /// are often on different objects of the same prop.
    /// </summary>
    public static bool IsCarrying(GameObject target)
    {
        if (target == null || Carried.Count == 0)
            return false;

        Transform t = target.transform;
        while (t != null)
        {
            if (Carried.Contains(t.gameObject))
                return true;
            t = t.parent;
        }

        return false;
    }

    /// <summary>True while the mouse has hold of anything at all.</summary>
    public static bool CarryingAnything => Carried.Count > 0;

    [Header("Reach")]
    [Tooltip("How far the cursor can reach, in metres. Generous by default — at a desk you are looking across the room, not standing at the cart.")]
    [SerializeField] private float reach = 8f;

    [Tooltip("What the cursor can touch. Leave as Everything unless props are being picked out of the wrong layer.")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [Tooltip("Let a click finish a scenario task directly, on things that are not props you can pick up — the EHR gates, the wristband. On means the whole scenario can be walked through with a mouse. Turn it off to force the scanner to be used, which is the fairer test of the scanner itself. Either way the scenario's focus gate still applies: clicking out of turn does nothing.")]
    [SerializeField] private bool clickCompletesScenarioTasks = true;

    [Header("Carrying")]
    [Tooltip("Put the held item down.")]
    [SerializeField] private Key dropKey = Key.Q;

    [Tooltip("How hard a carried prop chases the cursor. Higher is snappier and less stable.")]
    [SerializeField] private float carryStrength = 12f;

    [Tooltip("Ceiling on how fast a carried prop is allowed to move, in metres per second. Stops a prop yanked across the room from launching everything it clips.")]
    [SerializeField] private float carryMaxSpeed = 6f;

    [Tooltip("Closest and furthest a carried prop can be held, in metres. Scroll moves between them.")]
    [SerializeField] private Vector2 carryDistanceRange = new Vector2(0.35f, 3f);

    [Header("Moving around")]
    [Tooltip("Walk with WASD and look with right mouse held. Turn off if something else is driving the rig.")]
    [SerializeField] private bool allowMoveAndLook = true;

    [Tooltip("Metres per second.")]
    [SerializeField] private float walkSpeed = 2.5f;

    [Tooltip("Multiplier while shift is held.")]
    [SerializeField] private float runMultiplier = 2.5f;

    [Tooltip("Degrees per pixel of mouse movement.")]
    [SerializeField] private float lookSensitivity = 0.12f;

    [Header("Hands at a desk")]
    [Tooltip("Where the hands rest when nothing is tracking them, in the camera's local space: right/left, up/down, forward. Only used to rescue hands that are sitting on top of the camera — hands parked anywhere sensible are left alone. In a headset the tracked pose overwrites this every frame, so it is never seen there.")]
    [SerializeField] private Vector3 handRestPose = new Vector3(0.18f, -0.28f, 0.32f);

    [Header("Cursor")]
    [Tooltip("Draw a dot at the cursor, filled in when it is over something usable.")]
    [SerializeField] private bool drawReticle = true;

    [Tooltip("Also name what the cursor is over, next to the dot. Useful while wiring a scene up, noisy afterwards.")]
    [SerializeField] private bool drawHoverLabel;

    [Header("World-space UI")]
    [Tooltip("Give world-space canvases an event camera and a raycaster if they have neither, so the question panels can be clicked with the mouse. Only ever adds what is missing.")]
    [SerializeField] private bool fixWorldSpaceCanvases = true;

    private Camera eyeCamera;
    private Transform rigRoot;
    private CharacterController controller;
    private UnityEngine.InputSystem.XR.TrackedPoseDriver headDriver;

    private float pitch;
    private float yaw;
    private bool lookInitialised;

    private Rigidbody carriedBody;
    private bool carriedUsedGravity;
    private float carriedDrag;
    private float carriedAngularDrag;
    private float carryDistance;

    private GameObject hovered;
    private bool hoveredUsable;

    private void OnEnable()
    {
        Resolve();

        ParkHandsIfInsideHead();

        if (fixWorldSpaceCanvases)
            FixCanvases();
    }

    /// <summary>
    /// Move a hand out of the player's eye.
    ///
    /// The rig's hands are positioned every frame by a TrackedPoseDriver — but only while a
    /// device is actually reporting a pose. At a desk nothing does, so a hand authored at the
    /// origin of the camera offset stays exactly where the camera is: a 20 cm hand model at
    /// 0 cm, filling the view with two enormous fingers. That is what this rescues.
    ///
    /// Only hands sitting on top of the camera are moved. A hand parked anywhere deliberate is
    /// left alone, so this cannot quietly undo someone's authored rest pose.
    /// </summary>
    private void ParkHandsIfInsideHead()
    {
        Park(Rig.LeftHand, new Vector3(-handRestPose.x, handRestPose.y, handRestPose.z), "left");
        Park(Rig.RightHand, handRestPose, "right");
    }

    private void Park(Transform hand, Vector3 restPose, string side)
    {
        if (hand == null || eyeCamera == null)
            return;

        // On a headset the tracked pose overwrites this every frame anyway, and moving a hand
        // there would only be visible as a flicker on the first frame.
        if (DesktopPlayMode.HeadsetRunning)
            return;

        const float insideHead = 0.05f;
        if (Vector3.Distance(hand.position, eyeCamera.transform.position) > insideHead)
            return;

        hand.position = eyeCamera.transform.TransformPoint(restPose);
        hand.rotation = eyeCamera.transform.rotation;

        Debug.Log($"[DesktopMousePointer] The {side} hand was sitting on top of the camera — nothing is tracking it at a desk — so it has been parked in front of the player. Set a rest position on the rig's hand transform to choose where it sits.", this);
    }

    private void OnDisable()
    {
        Drop();

        if (headDriver != null)
            headDriver.enabled = true;
    }

    private void Resolve()
    {
        PlayerRig rig = Rig.XR;

        if (rig != null)
        {
            rigRoot = rig.transform;
            controller = rig.Controller;
            eyeCamera = rig.Head != null ? rig.Head.GetComponent<Camera>() : null;
        }

        if (eyeCamera == null)
            eyeCamera = Camera.main;

        if (rigRoot == null && eyeCamera != null)
            rigRoot = eyeCamera.transform.root;

        if (eyeCamera == null)
        {
            Debug.LogWarning("[DesktopMousePointer] No camera found, so the mouse has nothing to point through. Is there a rig in the scene?", this);
            enabled = false;
            return;
        }

        // With no XR device a TrackedPoseDriver writes nothing, but if one ever does appear
        // it would fight the mouse for the camera's rotation. Take it out of the loop while
        // the mouse is driving; OnDisable puts it back.
        //
        // Never while a headset is running, whatever brought this component up. Head tracking
        // is the one thing on the rig a player cannot give back to themselves, and switching it
        // off there pins the camera to the camera offset — which in Floor tracking mode is the
        // floor, so the player ends up looking along the ground. DesktopPlayMode is meant to
        // keep this component away from a headset entirely; this is the second lock on it.
        headDriver = eyeCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        if (headDriver != null && !DesktopPlayMode.HeadsetRunning)
            headDriver.enabled = false;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || eyeCamera == null)
            return;

        if (allowMoveAndLook)
            MoveAndLook(mouse);

        UpdateHover(mouse);

        if (Keyboard.current != null && Keyboard.current[dropKey].wasPressedThisFrame)
            Drop();

        if (carriedBody != null)
        {
            Vector2 scroll = mouse.scroll.ReadValue();
            if (Mathf.Abs(scroll.y) > 0.01f)
            {
                carryDistance = Mathf.Clamp(
                    carryDistance + Mathf.Sign(scroll.y) * 0.15f,
                    carryDistanceRange.x, carryDistanceRange.y);
            }

            // While carrying, a click belongs to whatever is being held — the scanner reads
            // it itself. Swapping props mid-scan would be worse than useless.
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
            Click();
    }

    private void FixedUpdate()
    {
        if (carriedBody == null || eyeCamera == null)
            return;

        Ray ray = CursorRay();
        Vector3 target = ray.origin + ray.direction * carryDistance;

        Vector3 delta = target - carriedBody.position;
        Vector3 velocity = delta * carryStrength;

        if (velocity.magnitude > carryMaxSpeed)
            velocity = velocity.normalized * carryMaxSpeed;

        carriedBody.linearVelocity = velocity;
        carriedBody.angularVelocity = Vector3.zero;
    }

    // ------------------------------------------------------------------------- pointing

    private Ray CursorRay()
    {
        Vector2 position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        return eyeCamera.ScreenPointToRay(position);
    }

    private void UpdateHover(Mouse mouse)
    {
        hovered = null;
        hoveredUsable = false;

        if (carriedBody != null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!Physics.Raycast(CursorRay(), out RaycastHit hit, reach, hitLayers, QueryTriggerInteraction.Ignore))
            return;

        hovered = hit.collider.gameObject;
        hoveredUsable = ResolveAction(hovered, out _, out _, out _);
    }

    /// <summary>
    /// What clicking <paramref name="target"/> would do. Grabbing wins over the other two:
    /// the scanner is both a prop you pick up and a scenario task, and a click that completed
    /// the task without putting the scanner in your hand would leave nothing to scan with.
    /// </summary>
    private bool ResolveAction(GameObject target, out Rigidbody grab, out Interact interact, out ScenarioTarget scenarioTarget)
    {
        grab = null;
        interact = null;
        scenarioTarget = null;

        if (target == null)
            return false;

        // Checked component-by-component rather than through a GrabHandle: this runs every
        // frame for the hover reticle, and a handle per frame is an allocation per frame.
        bool grabbable = target.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() != null
                      || target.GetComponentInParent<BNG.Grabbable>() != null;

        if (grabbable)
        {
            grab = target.GetComponentInParent<Rigidbody>();
            if (grab != null && !grab.isKinematic)
                return true;

            grab = null;
        }

        interact = target.GetComponentInParent<Interact>();
        if (interact != null)
            return true;

        if (!clickCompletesScenarioTasks)
            return false;

        scenarioTarget = target.GetComponentInParent<ScenarioTarget>();
        return scenarioTarget != null;
    }

    private void Click()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!Physics.Raycast(CursorRay(), out RaycastHit hit, reach, hitLayers, QueryTriggerInteraction.Ignore))
            return;

        if (!ResolveAction(hit.collider.gameObject, out Rigidbody grab, out Interact interact, out ScenarioTarget scenarioTarget))
            return;

        if (grab != null)
        {
            Pick(grab, hit.distance);
            return;
        }

        if (interact != null)
        {
            interact.TriggerInteract();
            return;
        }

        // Activate() checks the scenario is actually asking for this task, so an out-of-turn
        // click is silently ignored here exactly as it would be on device.
        scenarioTarget.Activate();
    }

    // -------------------------------------------------------------------------- carrying

    private void Pick(Rigidbody body, float distance)
    {
        Drop();

        carriedBody = body;
        carriedUsedGravity = body.useGravity;
        carriedDrag = body.linearDamping;
        carriedAngularDrag = body.angularDamping;
        carryDistance = Mathf.Clamp(distance, carryDistanceRange.x, carryDistanceRange.y);

        body.useGravity = false;
        body.linearDamping = 6f;
        body.angularDamping = 6f;

        Carried.Add(body.gameObject);
    }

    /// <summary>Put down whatever is being carried. Safe to call when nothing is.</summary>
    public void Drop()
    {
        if (carriedBody == null)
            return;

        Carried.Remove(carriedBody.gameObject);

        carriedBody.useGravity = carriedUsedGravity;
        carriedBody.linearDamping = carriedDrag;
        carriedBody.angularDamping = carriedAngularDrag;
        carriedBody.linearVelocity = Vector3.zero;
        carriedBody.angularVelocity = Vector3.zero;

        carriedBody = null;
    }

    // --------------------------------------------------------------------- move and look

    private void MoveAndLook(Mouse mouse)
    {
        Transform head = eyeCamera.transform;
        Transform body = rigRoot != null ? rigRoot : head;

        if (!lookInitialised)
        {
            yaw = body.eulerAngles.y;
            pitch = head.localEulerAngles.x;
            if (pitch > 180f)
                pitch -= 360f;
            lookInitialised = true;
        }

        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -85f, 85f);

            body.rotation = Quaternion.Euler(0f, yaw, 0f);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        Vector3 direction = Vector3.zero;
        if (keyboard.wKey.isPressed) direction += Vector3.forward;
        if (keyboard.sKey.isPressed) direction += Vector3.back;
        if (keyboard.aKey.isPressed) direction += Vector3.left;
        if (keyboard.dKey.isPressed) direction += Vector3.right;

        if (direction == Vector3.zero)
            return;

        float speed = walkSpeed * (keyboard.leftShiftKey.isPressed ? runMultiplier : 1f);

        // Walk where the head is looking, flattened, so looking at the floor does not drive
        // the player into it.
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(head.right, Vector3.up).normalized;
        Vector3 motion = (forward * direction.z + right * direction.x).normalized * (speed * Time.deltaTime);

        if (controller != null && controller.enabled)
            controller.Move(motion);
        else
            body.position += motion;
    }

    // ------------------------------------------------------------------------- world UI

    /// <summary>
    /// A world-space canvas is only clickable when it has an event camera and a raycaster.
    /// The question panels are authored for the hand ray, which supplies its own, so at a
    /// desk they can end up unclickable. Fill in only what is missing — adding a second
    /// raycaster to a panel that already has one would make every click land twice.
    /// </summary>
    private void FixCanvases()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int fixedCount = 0;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas.renderMode != RenderMode.WorldSpace)
                continue;

            bool changed = false;

            if (canvas.worldCamera == null && eyeCamera != null)
            {
                canvas.worldCamera = eyeCamera;
                changed = true;
            }

            if (canvas.GetComponent<BaseRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                changed = true;
            }

            if (changed)
                fixedCount++;
        }

        if (fixedCount > 0)
            Debug.Log($"[DesktopMousePointer] Made {fixedCount} world-space canvas(es) clickable with the mouse.", this);
    }

    // --------------------------------------------------------------------------- reticle

    private void OnGUI()
    {
        if (!drawReticle || Mouse.current == null)
            return;

        Vector2 position = Mouse.current.position.ReadValue();
        float x = position.x;
        float y = Screen.height - position.y;

        const float size = 8f;
        Color colour = carriedBody != null
            ? new Color(0.35f, 0.85f, 1f, 0.95f)
            : hoveredUsable ? new Color(0.35f, 1f, 0.45f, 0.95f)
                            : new Color(1f, 1f, 1f, 0.35f);

        Color previous = GUI.color;
        GUI.color = colour;
        GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), Texture2D.whiteTexture);

        if (drawHoverLabel && hovered != null)
            GUI.Label(new Rect(x + 12f, y - 10f, 320f, 20f), hovered.name);

        GUI.color = previous;
    }
}
