using UnityEngine;

/// <summary>
/// Parks a world-space panel in front of the player without bolting it to their face.
///
/// A caption has to be readable wherever the player is looking, but a panel rigidly welded
/// to the head is the classic VR comfort mistake: it never moves relative to the eye, so it
/// reads as dirt on the lens and there is no way to look away from it. This instead keeps a
/// dead zone — the panel simply stays put while the head turns inside it, and only once the
/// head has turned past the edge does the panel get dragged along, easing back to centre.
///
/// It also sits below the eye line rather than on it, so the caption never covers the thing
/// being talked about.
/// </summary>
[DisallowMultipleComponent]
public class CaptionPanelFollow : MonoBehaviour
{
    [Header("Who to follow")]
    [Tooltip("The player's head. Left empty, the main camera is used — that is the VR rig's Main Camera at runtime, since the scene's spare desktop camera is switched off.")]
    [SerializeField] private Transform head;

    [Header("Placement")]
    [Tooltip("Metres in front of the eyes. Below about 1.5 the eyes have to converge uncomfortably to read; beyond about 3 the text starts needing to be very large. 2 is the comfortable middle.")]
    [Range(0.75f, 5f)]
    [SerializeField] private float distance = 2f;

    [Tooltip("Degrees below the eye line. Captions belong in the lower field of view so they do not sit on top of the patient, the nurse or the terminal.")]
    [Range(0f, 40f)]
    [SerializeField] private float dropAngle = 14f;

    [Header("Follow")]
    [Tooltip("How far the head can turn left/right before the panel starts coming with it.")]
    [Range(0f, 45f)]
    [SerializeField] private float yawDeadzone = 12f;

    [Tooltip("How far the head can tilt up/down before the panel starts coming with it.")]
    [Range(0f, 45f)]
    [SerializeField] private float pitchDeadzone = 10f;

    [Tooltip("Roughly how long the panel takes to catch up once it is being dragged. Higher is calmer but laggier.")]
    [Range(0.02f, 1.5f)]
    [SerializeField] private float followSmoothTime = 0.3f;

    [Tooltip("How far the panel may end up above/below horizontal, so looking at your feet does not bury the caption in the floor.")]
    [SerializeField] private Vector2 pitchLimits = new Vector2(-20f, 30f);

    [Tooltip("Jump straight to centre whenever the panel is switched on. The player cannot see the jump — the panel was hidden — and it means every new line opens where they are already looking.")]
    [SerializeField] private bool centreOnShow = true;

    // Where the panel is anchored right now, as a yaw/pitch pair around the head. Kept as
    // angles rather than a position so the panel orbits the player at a fixed distance
    // instead of sliding around on a plane.
    private float yaw;
    private float pitch;
    private float yawVelocity;
    private float pitchVelocity;
    private bool headMissingReported;

    /// <summary>Re-centre on the next frame, as if the panel had just been switched on.</summary>
    public void Recentre()
    {
        if (!ResolveHead())
            return;

        ReadHeadAngles(out yaw, out pitch);
        yawVelocity = 0f;
        pitchVelocity = 0f;
        Place();
    }

    private void OnEnable()
    {
        if (centreOnShow)
            Recentre();
    }

    // LateUpdate, not Update: the head pose for this frame is written by the XR tracking
    // driver during Update, so following in Update would always be one frame stale.
    private void LateUpdate()
    {
        if (!ResolveHead())
            return;

        ReadHeadAngles(out float headYaw, out float headPitch);

        float targetYaw = DragTowards(yaw, headYaw, yawDeadzone, true);
        float targetPitch = DragTowards(pitch, headPitch, pitchDeadzone, false);

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, followSmoothTime);
        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, followSmoothTime);

        Place();
    }

    private void Place()
    {
        float placedPitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y) + dropAngle;

        // Roll is deliberately dropped: a caption that tilts with the head is unreadable and
        // is one of the fastest ways to make somebody queasy. The horizon always stays level.
        Quaternion facing = Quaternion.Euler(placedPitch, yaw, 0f);

        transform.SetPositionAndRotation(head.position + facing * (Vector3.forward * distance), facing);
    }

    private void ReadHeadAngles(out float headYaw, out float headPitch)
    {
        Vector3 forward = head.forward;

        headYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        headPitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// The anchor stays exactly where it is until the head has moved further than the dead
    /// zone, and from then on trails it by precisely the dead zone — so the panel is never
    /// yanked, it is pushed along by the edge of the window it lives in.
    /// </summary>
    private static float DragTowards(float current, float target, float deadzone, bool wrapsAround)
    {
        float delta = wrapsAround ? Mathf.DeltaAngle(current, target) : target - current;

        if (Mathf.Abs(delta) <= deadzone)
            return current;

        return current + delta - Mathf.Sign(delta) * deadzone;
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

        // Once only: this runs every frame, and a missing camera would otherwise bury the
        // console under the same line thousands of times.
        if (!headMissingReported)
        {
            headMissingReported = true;
            Debug.LogWarning($"[CaptionPanelFollow] '{name}' has no Head assigned and there is no enabled camera tagged MainCamera, so the caption panel cannot be placed. Drag the VR rig's Main Camera into Head.", this);
        }

        return false;
    }
}
