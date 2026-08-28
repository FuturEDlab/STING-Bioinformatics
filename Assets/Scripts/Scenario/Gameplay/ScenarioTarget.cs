using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// One component per interactive prop in the scenario. It is the single place that knows
/// "this object is how the player completes task X":
///
///  * it listens for the scenario asking for that task and glows while it is being asked for,
///  * it refuses interaction at any other time, so the player cannot scan the Allopurinol
///    during Scene 1 and skip half the story,
///  * and it raises the task back to the scenario once satisfied.
///
/// Put it on the scanner, the wristband, each medication bottle, and the EHR keyboard.
/// How the player satisfies it is up to <see cref="Trigger"/>: grab it, click it, touch it
/// with a hand, or scan it with the <see cref="ScannerTool"/>.
/// </summary>
public class ScenarioTarget : MonoBehaviour
{
    public enum TriggerMode
    {
        [InspectorName("Scan with the scanner")] Scan,
        [InspectorName("Grab / pick up")] Grab,
        [InspectorName("Click / press")] Click,
        [InspectorName("Script only (call Activate)")] Manual,

        // Appended rather than slotted in next to Click: the enum is serialized by index, so
        // inserting above would silently repoint every target already authored in a scene.
        [InspectorName("Touch with a hand")] Touch,
    }

    [Header("Scenario wiring (both channels are shared assets)")]
    [Tooltip("EV_BioTask — the channel the scenario's Wait For Task steps listen on.")]
    [SerializeField] private StringGameEvent taskChannel;

    [Tooltip("EV_Focus — broadcasts which task the scenario is waiting for right now.")]
    [SerializeField] private StringGameEvent focusChannel;

    [Tooltip("The task this object completes, e.g. methotrexate.scan. Must match the step asset exactly.")]
    [SerializeField] private string taskId;

    [Header("Behaviour")]
    [Tooltip("How the player completes it.")]
    [SerializeField] private TriggerMode trigger = TriggerMode.Scan;

    [Tooltip("Touch only: how close a hand has to get to the object, in metres. 0.08 is a fingertip's reach past the collider — big enough to hit a keyboard key without having to push through it.")]
    [Min(0.005f)]
    [SerializeField] private float touchRadius = 0.08f;

    [Tooltip("Only allow the interaction while the scenario is actually asking for this task. Turn off for props that should always be usable.")]
    [SerializeField] private bool requireFocus = true;

    [Tooltip("Complete only once. Off lets the object be used again later in the story (the EHR keyboard is used twice, but with two different task ids, so leave this on).")]
    [SerializeField] private bool oneShot = true;

    [Tooltip("The spot the scanner should aim at — put it on the barcode/label. Empty uses the collider's centre, or this object's position.")]
    [SerializeField] private Transform scanPoint;

    [Header("Feedback")]
    [Tooltip("Pulses while this task is the one being asked for. Auto-found on this object when empty.")]
    [SerializeField] private ScenarioHighlight highlight;

    [Tooltip("Played the moment the interaction succeeds — the script's Beep!")]
    [SerializeField] private AudioSource successSound;

    [Tooltip("Extra scene reactions on success (animation, EHR screen change, particle).")]
    [SerializeField] private UnityEvent onActivated;

    [Header("Desktop testing")]
    [Tooltip("Let a plain mouse click activate this in the editor, so the scenario can be walked through without a headset. Needs a Collider. Ignored in a build.")]
    [SerializeField] private bool allowMouseClickInEditor = true;

    private bool focused;
    private bool used;

    // Grab detection is polled rather than event-wired, and for the same reason it always
    // was: BNG puts its grab UnityEvents on a SEPARATE GrabbableUnityEvents component, and
    // XRI's are on the interactable, so an event hookup is easy to forget and silently does
    // nothing. GrabHandle just watches the held state, whichever rig — or the desktop mouse
    // pointer — is doing the holding, and needs no wiring at all.
    private GrabHandle grab;
    private bool wasHeld;

    public string TaskId => taskId;
    public bool IsFocused => focused;
    public bool IsUsed => used;

    /// <summary>True when interacting right now would actually do something.</summary>
    public bool CanActivate => !(oneShot && used) && (!requireFocus || focused);

    /// <summary>True when the scanner should treat this as a valid scan target.</summary>
    public bool AcceptsScan => trigger == TriggerMode.Scan && CanActivate;

    public StringGameEvent TaskChannel => taskChannel;
    public StringGameEvent FocusChannel => focusChannel;

    /// <summary>Where the scanner aims. Falls back to the collider centre, then the transform.</summary>
    public Vector3 ScanPoint
    {
        get
        {
            if (scanPoint != null)
                return scanPoint.position;

            if (cachedCollider == null)
                cachedCollider = GetComponentInChildren<Collider>();

            // bounds.center beats transform.position for imported props, whose pivot is
            // often at the floor or off to one side of the mesh.
            return cachedCollider != null ? cachedCollider.bounds.center : transform.position;
        }
    }

    private Collider cachedCollider;

    /// <summary>Plain-English reason this target is currently inert. For diagnostics.</summary>
    public string WhyNotActivatable()
    {
        if (oneShot && used)
            return "it has already been used once and One Shot is on.";
        if (requireFocus && !focused)
            return string.IsNullOrEmpty(taskId)
                ? "it has no Task Id set."
                : $"the scenario is not asking for '{taskId}' yet (Require Focus is on). Check the Focus Channel is the same asset on this object and on the ScenarioController.";
        return "no reason — it should be usable.";
    }

    private void Awake()
    {
        if (highlight == null)
            highlight = GetComponent<ScenarioHighlight>();

        if (trigger == TriggerMode.Grab)
        {
            // The grab component is usually on this object, but on an imported prop it can
            // sit on a parent or a child, so GrabHandle looks in all three places.
            grab = new GrabHandle(this);

            if (!grab.Exists)
                Debug.LogWarning($"[ScenarioTarget] '{name}' is set to complete on Grab, but there is no grab component on it, its parent, or its children — so being picked up can never be detected. Add an XRGrabInteractable (new hands) or a BNG Grabbable (army-guy rig), or switch Trigger to Click.", this);
        }

        // These two are the usual reason "nothing glows and nothing scans": an empty slot
        // is silent at runtime, so say it loudly at startup instead.
        if (focusChannel == null)
            Debug.LogWarning($"[ScenarioTarget] '{name}' has no Focus Channel. It will never glow, and with Require Focus on it will never accept input either. Assign EV_Focus.", this);

        if (taskChannel == null)
            Debug.LogWarning($"[ScenarioTarget] '{name}' has no Task Channel. Completing it would not tell the scenario. Assign EV_BioTask.", this);

        if (string.IsNullOrWhiteSpace(taskId))
            Debug.LogWarning($"[ScenarioTarget] '{name}' has no Task Id.", this);
    }

    /// <summary>
    /// Every enabled target in the scene. Lets the scanner find candidates by direction
    /// instead of depending on a collider being hit, and saves a scene-wide search.
    /// </summary>
    public static readonly List<ScenarioTarget> All = new List<ScenarioTarget>();

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);

        if (focusChannel != null)
            focusChannel.Subscribe(OnFocusChanged);
    }

    private void OnDisable()
    {
        All.Remove(this);

        if (focusChannel != null)
            focusChannel.Unsubscribe(OnFocusChanged);

        SetFocused(false);
    }

    private void OnFocusChanged(string focusedTaskId)
    {
        SetFocused(!string.IsNullOrEmpty(taskId) && focusedTaskId == taskId && !(oneShot && used));
    }

    private void Update()
    {
        if (trigger == TriggerMode.Touch)
        {
            CheckTouch();
            return;
        }

        if (trigger != TriggerMode.Grab || grab == null || !grab.Exists)
            return;

        bool held = grab.IsHeld;

        // Two ways this counts. The rising edge is the obvious one: the player picks it up
        // while the scenario is asking. The second matters just as much — the player often
        // grabs the scanner early, during the narration, and is ALREADY holding it by the
        // time the step comes round. Without that case the scenario would wait forever for
        // a pickup that already happened. `used` keeps either path to a single fire.
        if (held && (!wasHeld || (focused && !used)))
            Activate();

        wasHeld = held;
    }

    /// <summary>
    /// Hand proximity rather than a trigger collider: a trigger needs a Rigidbody, matching
    /// layers and a collider on the hand, and any one of the three being wrong fails
    /// silently. Distance to the collider's bounds needs nothing set up at all and reads the
    /// same on both rigs, because <see cref="Rig"/> hands the same two transforms back
    /// whichever one is driving.
    /// </summary>
    private void CheckTouch()
    {
        if (!CanActivate)
            return;

        if (HandWithinReach(Rig.LeftHand) || HandWithinReach(Rig.RightHand))
            Activate();
    }

    private bool HandWithinReach(Transform hand)
    {
        if (hand == null)
            return false;

        if (cachedCollider == null)
            cachedCollider = GetComponentInChildren<Collider>();

        // bounds, not Collider.ClosestPoint: the latter logs an error every frame on a
        // non-convex mesh collider, which is what an imported keyboard or monitor arrives as.
        Vector3 surface = cachedCollider != null
            ? cachedCollider.bounds.ClosestPoint(hand.position)
            : transform.position;

        return (hand.position - surface).sqrMagnitude <= touchRadius * touchRadius;
    }

    private void SetFocused(bool on)
    {
        // Only act on a real change. Several targets can sit on one prop — the EHR keyboard
        // carries one per gate — and they share a highlight. Without this, the targets that
        // are NOT being asked for would each switch the glow back off after the one that is
        // switched it on, and which of them ran last would decide whether the prop glowed.
        if (focused == on)
            return;

        focused = on;
        if (highlight != null)
            highlight.SetGlowing(on);
    }

    /// <summary>
    /// Complete this task. Wire it to a BNG Grabbable's onGrab, the project's
    /// Interact.onInteract, or a UI Button — or let <see cref="ScannerTool"/> call it.
    /// Silently does nothing when the scenario is not asking for this task yet.
    /// </summary>
    public void Activate()
    {
        if (!CanActivate)
        {
            // Not an error: the player poking at a prop out of turn is normal.
            return;
        }

        used = true;
        SetFocused(false);

        if (successSound != null)
            successSound.Play();

        onActivated?.Invoke();

        if (taskChannel != null && !string.IsNullOrEmpty(taskId))
            taskChannel.Raise(taskId);
        else
            Debug.LogWarning($"[ScenarioTarget] '{name}' has no task channel or task id assigned; nothing was raised.", this);
    }

    /// <summary>Re-arm a one-shot target (used when the scenario restarts).</summary>
    public void ResetTarget()
    {
        used = false;
        SetFocused(false);
    }

    // --- Grab support -----------------------------------------------------------------
    // BNG's Grabbable exposes UnityEvents in the Inspector; wiring onGrab -> Activate() is
    // the documented route. This is the code-side equivalent for objects that would rather
    // not carry an extra event hookup.

    /// <summary>Call from a Grabbable's onGrab event when trigger is set to Grab.</summary>
    public void OnGrabbed()
    {
        if (trigger == TriggerMode.Grab)
            Activate();
    }

    /// <summary>Call from a button / pointer click when trigger is set to Click.</summary>
    public void OnClicked()
    {
        if (trigger == TriggerMode.Click)
            Activate();
    }

    /// <summary>Called by <see cref="ScannerTool"/>. Returns true when the scan counted.</summary>
    public bool OnScanned()
    {
        if (!AcceptsScan)
            return false;

        Activate();
        return true;
    }

    private void OnMouseDown()
    {
#if UNITY_EDITOR
        // Desktop shortcut so the whole scenario can be played through in the editor
        // without putting the headset on. Requires a Collider on this object.
        if (allowMouseClickInEditor)
            Activate();
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Activate (test)")]
    private void ActivateFromMenu()
    {
        // Bypasses the focus gate so a single prop can be tested in isolation.
        used = false;
        focused = true;
        Activate();
    }
#endif
}
