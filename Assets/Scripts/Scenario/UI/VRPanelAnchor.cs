using UnityEngine;

/// <summary>
/// Puts a world-space panel in front of the player at the moment it opens, at a size and
/// distance chosen in metres rather than in canvas pixels.
///
/// A panel parked at fixed world coordinates was fine when the only camera was the one the
/// scene was authored through. In a headset the player stands where the rig stands and looks
/// where they like, so a panel left at its authored spot is typically metres away, off to one
/// side, and quite possibly on the far side of a wall — present in the scene, invisible in
/// practice.
///
/// Unlike <see cref="CaptionPanelFollow"/> this does NOT follow the head. The panel is
/// answered by pointing at it, and a target that drifts while being aimed at is far worse
/// than one that stays put — so it is placed once, on open, and then left alone.
/// </summary>
[DisallowMultipleComponent]
public class VRPanelAnchor : MonoBehaviour
{
    public enum PlacementMode
    {
        VRHeadPlacement,
        CustomWorldPlacement
    }

    [Header("Placement Mode")]
    [Tooltip("Choose whether the panel opens relative to the player's head or at a fixed world location.")]
    [SerializeField] private PlacementMode placementMode = PlacementMode.VRHeadPlacement;

    [Header("Who to place in front of")]
    [Tooltip("The player's head. Left empty, the main camera is used — the XR rig's camera at runtime.")]
    [SerializeField] private Transform head;

    [Header("Placement")]
    [Tooltip("Metres in front of the player. Close enough to read and to point at comfortably, far enough that a panel this size does not have to be scanned by turning the head.")]
    [Range(0.6f, 5f)]
    [SerializeField] private float distance = 1.7f;

    [Tooltip("Metres above (or below) eye height for the panel's centre. Slightly negative keeps the top of a tall panel out of the ceiling.")]
    [SerializeField] private float heightOffset = -0.15f;

    [Header("Custom World Placement")]
    [Tooltip("World position used when Custom World Placement is selected.")]
    [SerializeField] private Vector3 customPlacementPosition;

    [Tooltip("World rotation, in Euler angles, used when Custom World Placement is selected.")]
    [SerializeField] private Vector3 customPlacementEulerAngles;

    [Tooltip("How wide the panel should end up, in metres. The canvas is authored at 2000 px square, which is 2 m at its current scale — big enough that the player has to sweep their head across it. 0 leaves the scale untouched.")]
    [SerializeField] private float panelWidthMetres = 1.4f;

    [Header("While it is open")]
    [Tooltip("Re-place the panel if the player wanders further than this from it, in metres, so walking away cannot strand a panel the scenario is waiting on. 0 switches this off.")]
    [SerializeField] private float replaceIfFurtherThan = 3.5f;

    [Tooltip("Place as soon as this component is switched on. Leave off when something else calls Place explicitly, which is how the question panel drives it.")]
    [SerializeField] private bool placeOnEnable;

    [Tooltip("Hand this panel's canvas to BNG's VR UI system when it opens, so the hand laser can click it. The UI system only sweeps the scene for canvases once, at startup, and misses anything switched off at that moment.")]
    [SerializeField] private bool registerWithVrUi = true;

    [Tooltip("Log where the panel was put, and whether the laser can reach it, each time it opens. Answers 'the panel never appeared' without a headset on.")]
    [SerializeField] private bool logPlacement = true;

    private bool placed;
    private bool headMissingReported;

    /// <summary>True between <see cref="Place"/> and <see cref="Release"/> — i.e. while the panel is open.</summary>
    public bool IsPlaced => placed;

    /// <summary>Move the panel in front of the player and size it. Safe to call repeatedly.</summary>
    [ContextMenu("Place In Front Of Player")]
    public void Place()
    {
        if (placementMode == PlacementMode.CustomWorldPlacement)
        {
            transform.SetPositionAndRotation(
                customPlacementPosition,
                Quaternion.Euler(customPlacementEulerAngles));
        }
        else
        {
            if (!ResolveHead())
                return;

            Vector3 forward = FlattenedFacing();

            Vector3 position = head.position + forward * distance + Vector3.up * heightOffset;

            // The canvas reads correctly when its own forward points the same way the player is
            // looking, i.e. away from them — not back at them.
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        }

        ApplyWidth();
        bool laserReady = RegisterForLaser();
        placed = true;

            if (logPlacement)
            {
                string placementDescription = placementMode == PlacementMode.CustomWorldPlacement
                    ? $"custom world position {transform.position}"
                    : $"{distance} m in front of '{head.name}' at {head.position}";
                Debug.Log($"[VRPanelAnchor] '{name}' placed at {placementDescription}, {panelWidthMetres} m wide. Laser: {(laserReady ? "canvas registered with the VR UI system" : "NO VR UI system in the scene, so the hand laser cannot click this") }.", this);
            }
    }

    /// <summary>
    /// A world-space canvas is only clickable by the hand laser once the VR UI system has
    /// given it an event camera, and that sweep happens once at startup. A panel that was
    /// hidden then — which this one is, for most of the simulation — is never picked up, so
    /// it is handed over here instead, every time it opens.
    /// </summary>
    private bool RegisterForLaser()
    {
        if (!registerWithVrUi)
            return true;

        Canvas canvas = GetComponent<Canvas>();

        if (canvas == null)
            return false;

        // Deliberately not VRUISystem.Instance: that getter builds an unconfigured module on
        // the fly if none exists, and a half-wired input module is harder to diagnose than a
        // missing one.
        BNG.VRUISystem system = FindAnyObjectByType<BNG.VRUISystem>(FindObjectsInactive.Include);

        if (system == null)
            return false;

        system.AddCanvas(canvas);
        return true;
    }

    /// <summary>Stop watching the player. Call it when the panel closes.</summary>
    public void Release()
    {
        placed = false;
    }

    private void OnEnable()
    {
        if (placeOnEnable)
            Place();
    }

    private void Update()
    {
        if (!placed || placementMode != PlacementMode.VRHeadPlacement || replaceIfFurtherThan <= 0f)
            return;

        if (head == null && !ResolveHead())
            return;

        // Only the horizontal gap matters: crouching or standing on tiptoe should not throw
        // the panel across the room.
        Vector3 offset = transform.position - head.position;
        offset.y = 0f;

        if (offset.magnitude > replaceIfFurtherThan)
            Place();
    }

    private void ApplyWidth()
    {
        if (panelWidthMetres <= 0f)
            return;

        if (transform is not RectTransform rect)
            return;

        float authoredWidth = rect.rect.width;

        if (authoredWidth <= 0f)
            return;

        // The panel is normally a root canvas with no scaled parent, but dividing it out
        // means the requested width is honoured either way.
        float parentScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;

        if (Mathf.Approximately(parentScale, 0f))
            return;

        transform.localScale = Vector3.one * (panelWidthMetres / authoredWidth / parentScale);
    }

    /// <summary>
    /// Which way the player is facing, on the level. Flattening keeps the panel upright: a
    /// panel tipped to match a head that happens to be looking at the floor is unreadable.
    /// </summary>
    private Vector3 FlattenedFacing()
    {
        Vector3 forward = head.forward;
        forward.y = 0f;

        // Looking straight up or down leaves nothing to flatten. The top of the head is
        // horizontal in exactly that pose, so it stands in — negated when looking up, where
        // it points backwards.
        if (forward.sqrMagnitude < 1e-4f)
        {
            forward = head.up;
            forward.y = 0f;

            if (head.forward.y > 0f)
                forward = -forward;
        }

        return forward.sqrMagnitude < 1e-4f ? Vector3.forward : forward.normalized;
    }

    private bool ResolveHead()
    {
        if (head != null)
            return true;

        Camera main = Camera.main;

        if (main != null)
        {
            head = main.transform;
            return true;
        }

        if (!headMissingReported)
        {
            headMissingReported = true;
            Debug.LogWarning($"[VRPanelAnchor] '{name}' has no Head assigned and there is no enabled camera tagged MainCamera, so the panel cannot be placed and will stay wherever it was left in the scene.", this);
        }

        return false;
    }
}
