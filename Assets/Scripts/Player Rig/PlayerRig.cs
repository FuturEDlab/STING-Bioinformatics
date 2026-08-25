using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// The one place the rest of the game asks "where is the player?".
///
/// Gameplay scripts used to reach into the BNG rig for this — <c>InputBridge.Instance</c>,
/// <c>BNGPlayerController</c>, the <c>HandController</c> transforms — which hard-wired every
/// one of them to that specific prefab. They now go through this component instead, so the
/// rig underneath can be swapped without touching gameplay code again.
///
/// Put it on the root of the player prefab (the object carrying <see cref="XROrigin"/>).
/// Everything is auto-resolved in <see cref="Awake"/>, so the Inspector fields only need
/// filling in when the hierarchy is unusual.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class PlayerRig : MonoBehaviour
{
    private static PlayerRig instance;

    /// <summary>
    /// The rig in the loaded scene, or null when there isn't one. Falls back to a scene
    /// search so scripts whose Awake runs before the rig's still find it.
    /// </summary>
    public static PlayerRig Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<PlayerRig>(FindObjectsInactive.Exclude);
            return instance;
        }
    }

    [Header("Rig (auto-filled on Awake — only set these if the hierarchy is unusual)")]
    [Tooltip("The XR Origin. Left empty it is taken from this object.")]
    [SerializeField] private XROrigin origin;

    [Tooltip("The head/camera transform. Left empty it is taken from the XR Origin's camera.")]
    [SerializeField] private Transform head;

    [Tooltip("Left hand transform — the controller anchor, not the hand model.")]
    [SerializeField] private Transform leftHand;

    [Tooltip("Right hand transform — the controller anchor, not the hand model.")]
    [SerializeField] private Transform rightHand;

    [Tooltip("The capsule that stops the player walking through walls. Left empty it is searched for on this object and its children.")]
    [SerializeField] private CharacterController body;

    [Tooltip("Fades the view to black. Left empty one is added to the head on first use, so teleport fades work with no setup.")]
    [SerializeField] private ScreenFade fade;

    /// <summary>The XR Origin this rig is built on.</summary>
    public XROrigin Origin => origin;

    /// <summary>The head. Use this for anything that should track where the player is looking.</summary>
    public Transform Head => head;

    /// <summary>Left controller/hand. Null until the rig has resolved it.</summary>
    public Transform LeftHand => leftHand;

    /// <summary>Right controller/hand. Null until the rig has resolved it.</summary>
    public Transform RightHand => rightHand;

    /// <summary>
    /// Where the player is standing, at floor level. This is the character capsule's
    /// transform — the closest equivalent to the old BNGPlayerController transform, and what
    /// proximity checks should measure against.
    /// </summary>
    public Transform Body => body != null ? body.transform : transform;

    /// <summary>The character capsule. Null if the rig has no CharacterController.</summary>
    public CharacterController Controller => body;

    /// <summary>The collider gameplay code should tell props to ignore.</summary>
    public Collider BodyCollider => body;

    /// <summary>The screen fader, created on the head the first time something asks for it.</summary>
    public ScreenFade Fade
    {
        get
        {
            if (fade == null && head != null)
                fade = head.GetComponentInChildren<ScreenFade>(true) ?? head.gameObject.AddComponent<ScreenFade>();
            return fade;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[PlayerRig] '{name}' is a second rig in the scene — '{instance.name}' is already the active one. Delete one of them; gameplay only ever talks to the first.", this);
            return;
        }

        instance = this;
        Resolve();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    /// <summary>
    /// Fills in whatever was left empty in the Inspector. Safe to call again — it only ever
    /// writes fields that are still null.
    /// </summary>
    public void Resolve()
    {
        if (origin == null)
            origin = GetComponent<XROrigin>() ?? GetComponentInChildren<XROrigin>(true);

        if (head == null && origin != null && origin.Camera != null)
            head = origin.Camera.transform;

        if (head == null)
        {
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null)
                head = cam.transform;
        }

        if (body == null)
            body = GetComponent<CharacterController>() ?? GetComponentInChildren<CharacterController>(true);

        // The hands are whatever the interactors are parented to. Reading them off the
        // interactors rather than by name means renaming the hand objects can't break this.
        if (leftHand == null || rightHand == null)
            ResolveHands();

        if (head == null)
            Debug.LogWarning($"[PlayerRig] '{name}' could not find a camera. Anything that measures distance to the player will do nothing.", this);
    }

    private void ResolveHands()
    {
        XRBaseInteractor[] interactors = GetComponentsInChildren<XRBaseInteractor>(true);
        foreach (XRBaseInteractor interactor in interactors)
        {
            if (interactor.handedness == InteractorHandedness.None) continue;
            if (interactor.handedness == InteractorHandedness.Left && leftHand != null) continue;
            if (interactor.handedness == InteractorHandedness.Right && rightHand != null) continue;

            // The interactor usually hangs off the hand rather than being on it, so walk up
            // to the object the tracking driver is on.
            TrackedPoseDriver driver = interactor.GetComponentInParent<TrackedPoseDriver>();
            Transform hand = driver != null ? driver.transform : interactor.transform;

            if (interactor.handedness == InteractorHandedness.Left)
                leftHand = hand;
            else
                rightHand = hand;
        }

        if (leftHand == null || rightHand == null)
            Debug.LogWarning($"[PlayerRig] '{name}' could not resolve both hands from its interactors. Set Left Hand / Right Hand on this component by hand, or set Handedness on the interactors.", this);
    }

    /// <summary>
    /// Puts the player's feet at <paramref name="footPosition"/>, optionally turning them to
    /// face <paramref name="facing"/>.
    ///
    /// This moves the rig so the <em>camera</em> lands on the spot, not the origin — with room
    /// scale the two are metres apart once the player has walked around their play space, and
    /// moving the origin would drop them somewhere they didn't ask to go. The character
    /// capsule is switched off across the move so it can't resolve a collision against the
    /// position it was moved out of.
    /// </summary>
    /// <param name="footPosition">World position for the player's feet.</param>
    /// <param name="facing">Direction the player should end up looking, flattened to horizontal. Pass null to keep their current facing.</param>
    public void TeleportTo(Vector3 footPosition, Vector3? facing)
    {
        if (origin == null)
        {
            Debug.LogWarning($"[PlayerRig] '{name}' has no XR Origin, so it cannot be teleported.", this);
            return;
        }

        bool hadController = body != null && body.enabled;
        if (hadController)
            body.enabled = false;

        if (facing.HasValue)
        {
            Vector3 flat = Vector3.ProjectOnPlane(facing.Value, Vector3.up);
            if (flat.sqrMagnitude > 0.0001f)
                origin.MatchOriginUpCameraForward(Vector3.up, flat.normalized);
        }

        // Keep the player's real head height — dropping them at floor level would bury the
        // camera in the ground.
        float eyeHeight = origin.CameraInOriginSpaceHeight;
        origin.MoveCameraToWorldLocation(footPosition + (Vector3.up * eyeHeight));

        Rigidbody rb = origin.Origin != null ? origin.Origin.GetComponent<Rigidbody>() : null;
        if (rb != null)
            rb.linearVelocity = Vector3.zero;

        if (hadController)
            body.enabled = true;
    }
}
