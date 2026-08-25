using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The barcode scanner. Casts a beam out of its nose; when that beam finds a
/// <see cref="ScenarioTarget"/> the scenario is currently asking to be scanned, pulling
/// the trigger completes it — the wristband, then each medication bottle.
///
/// Aiming is deliberately forgiving. Holding a tracked controller steady on a small bottle
/// label is not the skill being taught here, so detection defaults to a **cone**: any
/// scannable roughly in front of the scanner counts, whether or not a raycast happens to
/// land on its collider. The physics cast is still available for a stricter feel.
///
/// The beam direction is adjustable from the Inspector via <c>Aim Rotation</c>, so an
/// imported model whose forward axis is not +Z can be corrected without creating and
/// rotating a child transform. Gizmos draw the beam live in the Scene view while you tune it.
/// </summary>
public class ScannerTool : MonoBehaviour
{
    public enum DetectionMode
    {
        [InspectorName("Cone — most forgiving (recommended)")] Cone,
        [InspectorName("Physics cast — needs colliders")] Physics,
        [InspectorName("Both — cast first, then cone")] Both,
    }

    [Header("Aim — tune these with the Scene view open")]
    [Tooltip("Where the beam leaves the scanner. Empty uses this object's own transform.")]
    [SerializeField] private Transform muzzle;

    [Tooltip("Rotates the beam, in degrees, relative to the muzzle. Use this to point the beam out of the scanner's nose when the model's forward axis isn't +Z — no child object needed. Watch the yellow gizmo as you drag.")]
    [SerializeField] private Vector3 aimRotation = Vector3.zero;

    [Tooltip("Shifts the beam's starting point, in local space. Use it to move the origin to the tip of the scanner.")]
    [SerializeField] private Vector3 aimOffset = Vector3.zero;

    [Tooltip("How far the scanner reaches, in metres.")]
    [SerializeField] private float range = 0.75f;

    [Header("Detection")]
    [Tooltip("Cone ignores colliders entirely and just asks 'is a scannable roughly in front of me?'. Physics needs the target to have a collider the beam can hit.")]
    [SerializeField] private DetectionMode detectionMode = DetectionMode.Cone;

    [Tooltip("Cone mode: how far off-centre a target can be and still count. 25-40 degrees feels natural in VR; raise it if aiming feels fussy.")]
    [Range(1f, 90f)]
    [SerializeField] private float coneAngle = 30f;

    [Tooltip("Cone mode: ignore targets hidden behind geometry. Leave off unless players are scanning bottles through the cart.")]
    [SerializeField] private bool requireLineOfSight = false;

    [Tooltip("Physics mode: aim forgiveness — the cast is this wide.")]
    [SerializeField] private float beamRadius = 0.06f;

    [Tooltip("Physics mode: what the beam can hit.")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Trigger")]
    [Tooltip("Require the scanner to be held before it will fire. Ignored when the object cannot be picked up at all, so a non-grabbable scanner still works. Being carried by the desktop mouse pointer counts as held.")]
    [SerializeField] private bool mustBeHeld = true;

    [Tooltip("Fire as soon as the beam finds a valid target, with no trigger pull. The easiest way to test without a headset.")]
    [SerializeField] private bool autoScanOnAim = false;

    [Tooltip("Seconds a target must stay lined up before an auto-scan fires. Stops the scanner going off as the beam sweeps past something.")]
    [SerializeField] private float autoScanDwell = 0.35f;

    [Tooltip("Desktop fallback: this key also fires the scanner. Set to None to disable.")]
    [SerializeField] private KeyCode keyboardScanKey = KeyCode.E;

    [Tooltip("Desktop fallback: left mouse button also fires the scanner.")]
    [SerializeField] private bool mouseButtonScans = true;

    [Tooltip("Seconds before the same scanner can fire again.")]
    [SerializeField] private float cooldown = 0.75f;

    [Header("Feedback")]
    [Tooltip("Optional line renderer showing the beam while the scanner is held.")]
    [SerializeField] private LineRenderer beam;

    [Tooltip("Beam colour when nothing scannable is lined up.")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.25f);

    [Tooltip("Beam colour when a valid target is lined up.")]
    [SerializeField] private Color lockedColor = new Color(0.3f, 1f, 0.4f, 0.9f);

    [Tooltip("Played on a successful scan if the target has no sound of its own.")]
    [SerializeField] private AudioSource beep;

    [Tooltip("Raised on every successful scan — hook the EHR screen update here.")]
    [SerializeField] private UnityEvent onScanned;

    [Header("Debug")]
    [Tooltip("Log what the beam finds and why a scan was refused. Turn this on first when scanning is not working.")]
    [SerializeField] private bool debugLogging;

    [Tooltip("Draw the beam and cone in the Scene view even when this object is not selected.")]
    [SerializeField] private bool alwaysDrawGizmos = true;

    private GrabHandle grab;
    private float nextScanTime;
    private float dwellTimer;
    private ScenarioTarget lastLogged;
    private bool warnedNoGrabbable;

    private Transform Muzzle => muzzle != null ? muzzle : transform;

    /// <summary>Where the beam starts, with the Inspector offset applied.</summary>
    public Vector3 BeamOrigin => Muzzle.TransformPoint(aimOffset);

    /// <summary>Which way the beam points, with the Inspector rotation applied.</summary>
    public Vector3 BeamDirection => Muzzle.rotation * Quaternion.Euler(aimRotation) * Vector3.forward;

    /// <summary>The scannable currently lined up, or null.</summary>
    public ScenarioTarget CurrentTarget { get; private set; }

    private void Awake()
    {
        // GrabHandle answers "is this held?" for a BNG Grabbable, an XRI XRGrabInteractable,
        // or the desktop mouse pointer, so the scanner works on either rig and at a desk
        // without knowing which is which.
        grab = new GrabHandle(this);
    }

    /// <summary>
    /// Whether the scanner is live. A scanner that cannot be picked up at all can never
    /// report "being held", so requiring that would make it permanently inert — treat it as
    /// held instead and say so once.
    /// </summary>
    private bool IsHeld()
    {
        if (!mustBeHeld)
            return true;

        if (!grab.Exists)
        {
            if (!warnedNoGrabbable)
            {
                warnedNoGrabbable = true;
                Debug.LogWarning($"[ScannerTool] '{name}' has 'Must Be Held' ticked but carries no grab component, so there is nothing to hold. Treating it as always held. Add an XRGrabInteractable (new hands) or a BNG Grabbable (army-guy rig), or untick Must Be Held to silence this.", this);
            }
            return true;
        }

        return grab.IsHeld;
    }

    private void Update()
    {
        if (!IsHeld())
        {
            CurrentTarget = null;
            dwellTimer = 0f;
            ShowBeam(false, Vector3.zero);
            return;
        }

        ScenarioTarget found = FindTarget(out Vector3 endPoint);

        // Reset the dwell timer whenever the lock changes, so sweeping past a bottle on the
        // way to another one can't fire the scanner at the wrong thing.
        if (found != CurrentTarget)
            dwellTimer = 0f;
        else if (found != null)
            dwellTimer += Time.deltaTime;

        CurrentTarget = found;
        ShowBeam(true, endPoint);

        if (debugLogging && CurrentTarget != lastLogged)
        {
            lastLogged = CurrentTarget;
            if (CurrentTarget != null)
                Debug.Log($"[ScannerTool] Locked on '{CurrentTarget.name}' (task '{CurrentTarget.TaskId}'). Pull the trigger, press {keyboardScanKey}, or click.", CurrentTarget);
        }

        if (CurrentTarget == null || Time.time < nextScanTime)
            return;

        if (TriggerPressed() || (autoScanOnAim && dwellTimer >= autoScanDwell))
            TryScan();
    }

    /// <summary>
    /// Scan whatever is lined up. Public so a UI button or a debug key can fire the
    /// scanner without a controller.
    /// </summary>
    public void TryScan()
    {
        ScenarioTarget target = CurrentTarget != null ? CurrentTarget : FindTarget(out _);

        if (target == null)
        {
            if (debugLogging)
                Debug.Log($"[ScannerTool] Nothing scannable found. Mode is {detectionMode}; check the beam direction (yellow gizmo) and that the scenario is currently asking for a scan.", this);
            return;
        }

        if (Time.time < nextScanTime)
            return;

        if (!target.OnScanned())
        {
            if (debugLogging)
                Debug.Log($"[ScannerTool] '{target.name}' refused the scan — {target.WhyNotActivatable()}", target);
            return;
        }

        nextScanTime = Time.time + cooldown;
        dwellTimer = 0f;

        // The target plays its own sound when it has one, so don't double up.
        if (beep != null)
            beep.Play();

        onScanned?.Invoke();
    }

    private ScenarioTarget FindTarget(out Vector3 endPoint)
    {
        endPoint = BeamOrigin + (BeamDirection * range);

        if (detectionMode != DetectionMode.Cone)
        {
            ScenarioTarget hit = FindByPhysics(out Vector3 hitPoint);
            if (hit != null)
            {
                endPoint = hitPoint;
                return hit;
            }

            if (detectionMode == DetectionMode.Physics)
                return null;
        }

        ScenarioTarget coneHit = FindByCone();
        if (coneHit != null)
            endPoint = coneHit.ScanPoint;

        return coneHit;
    }

    /// <summary>
    /// Strict mode: sweep a sphere along the beam. Uses SphereCastAll rather than the
    /// single nearest hit, because the nearest collider is often the cart or the patient's
    /// arm rather than the thing being aimed at — taking only the first hit made scanning
    /// feel broken whenever anything sat in front of the label.
    /// </summary>
    private ScenarioTarget FindByPhysics(out Vector3 hitPoint)
    {
        Vector3 origin = BeamOrigin;
        Vector3 direction = BeamDirection;
        hitPoint = origin + (direction * range);

        // A spherecast ignores anything already overlapping it at the start, so begin
        // slightly behind the muzzle — otherwise a bottle pressed against the nose of the
        // scanner registers as nothing at all.
        Vector3 castStart = origin - (direction * beamRadius);

        RaycastHit[] hits = UnityEngine.Physics.SphereCastAll(
            castStart, beamRadius, direction, range + beamRadius, hitLayers, QueryTriggerInteraction.Collide);

        ScenarioTarget best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            ScenarioTarget t = hits[i].collider.GetComponentInParent<ScenarioTarget>();
            if (t == null || !t.AcceptsScan)
                continue;

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                best = t;
                hitPoint = hits[i].point;
            }
        }

        if (best == null && debugLogging && hits.Length > 0)
            Debug.Log($"[ScannerTool] Beam passed through {hits.Length} collider(s), none of them a scannable the scenario is asking for.", this);

        return best;
    }

    /// <summary>
    /// Forgiving mode: pick the scannable closest to the beam's axis, ignoring colliders.
    /// This is what makes aiming at a small bottle label practical with a tracked
    /// controller, and it works even on props with no collider at all.
    /// </summary>
    private ScenarioTarget FindByCone()
    {
        Vector3 origin = BeamOrigin;
        Vector3 direction = BeamDirection;

        ScenarioTarget best = null;
        float bestAngle = coneAngle;

        for (int i = 0; i < ScenarioTarget.All.Count; i++)
        {
            ScenarioTarget t = ScenarioTarget.All[i];
            if (t == null || !t.AcceptsScan)
                continue;

            Vector3 toTarget = t.ScanPoint - origin;
            float distance = toTarget.magnitude;
            if (distance > range || distance < 0.0001f)
                continue;

            float angle = Vector3.Angle(direction, toTarget);
            if (angle > bestAngle)
                continue;

            if (requireLineOfSight && UnityEngine.Physics.Linecast(origin, t.ScanPoint, out RaycastHit blocker, hitLayers, QueryTriggerInteraction.Ignore))
            {
                // Something solid in the way — unless it belongs to the target itself.
                if (blocker.collider.GetComponentInParent<ScenarioTarget>() != t)
                    continue;
            }

            bestAngle = angle;
            best = t;
        }

        return best;
    }

    private void ShowBeam(bool visible, Vector3 endPoint)
    {
        if (beam == null)
            return;

        beam.enabled = visible;
        if (!visible)
            return;

        beam.positionCount = 2;
        beam.SetPosition(0, BeamOrigin);
        beam.SetPosition(1, endPoint);

        Color c = CurrentTarget != null ? lockedColor : idleColor;
        beam.startColor = c;
        beam.endColor = c;
    }

    private bool TriggerPressed()
    {
        // Desktop fallbacks first: with no rig at all the controller path reads as nothing,
        // so the key and the mouse click are what make the scanner testable at a desk.
        if (keyboardScanKey != KeyCode.None && Input.GetKeyDown(keyboardScanKey))
            return true;

        if (mouseButtonScans && Input.GetMouseButtonDown(0))
            return true;

        return Rig.LeftTriggerDown || Rig.RightTriggerDown;
    }

    // ---------------------------------------------------------------- editor helpers

    private void OnDrawGizmos()
    {
        if (alwaysDrawGizmos)
            DrawAimGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!alwaysDrawGizmos)
            DrawAimGizmos();
    }

    /// <summary>Draws the beam and its cone so Aim Rotation can be dialled in visually.</summary>
    private void DrawAimGizmos()
    {
        Vector3 origin = BeamOrigin;
        Vector3 direction = BeamDirection;
        Vector3 end = origin + (direction * range);

        bool locked = Application.isPlaying && CurrentTarget != null;
        Gizmos.color = locked ? Color.green : Color.yellow;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(origin, 0.01f);

        if (detectionMode == DetectionMode.Physics)
        {
            Gizmos.DrawWireSphere(end, beamRadius);
            return;
        }

        // Four spokes are enough to read the cone's width without cluttering the view.
        float radius = Mathf.Tan(coneAngle * Mathf.Deg2Rad) * range;
        Vector3 up = Vector3.Cross(direction, Vector3.right).normalized;
        if (up.sqrMagnitude < 0.001f)
            up = Vector3.Cross(direction, Vector3.forward).normalized;
        Vector3 right = Vector3.Cross(direction, up).normalized;

        Gizmos.color = new Color(1f, 0.92f, 0.2f, 0.35f);
        Gizmos.DrawLine(origin, end + (up * radius));
        Gizmos.DrawLine(origin, end - (up * radius));
        Gizmos.DrawLine(origin, end + (right * radius));
        Gizmos.DrawLine(origin, end - (right * radius));

        if (Application.isPlaying && CurrentTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, CurrentTarget.ScanPoint);
            Gizmos.DrawWireSphere(CurrentTarget.ScanPoint, 0.03f);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Sets Aim Rotation so the beam points straight at the nearest scannable. Saves
    /// eyeballing the angles when the model's forward axis is somewhere unhelpful.
    /// </summary>
    [ContextMenu("Aim At Nearest Scannable")]
    private void AimAtNearestScannable()
    {
        ScenarioTarget[] all = FindObjectsByType<ScenarioTarget>(FindObjectsSortMode.None);
        ScenarioTarget nearest = null;
        float best = float.MaxValue;

        foreach (ScenarioTarget t in all)
        {
            float d = Vector3.Distance(BeamOrigin, t.ScanPoint);
            if (d < best) { best = d; nearest = t; }
        }

        if (nearest == null)
        {
            Debug.LogWarning("[ScannerTool] No ScenarioTarget in the scene to aim at.", this);
            return;
        }

        Vector3 toTarget = nearest.ScanPoint - BeamOrigin;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("[ScannerTool] The nearest scannable is on top of the muzzle, so there is no direction to aim. Move the scanner away from it first.", this);
            return;
        }

        UnityEditor.Undo.RecordObject(this, "Aim Scanner");
        Vector3 local = Muzzle.InverseTransformDirection(toTarget.normalized);
        aimRotation = Quaternion.LookRotation(local).eulerAngles;
        UnityEditor.EditorUtility.SetDirty(this);

        Debug.Log($"[ScannerTool] Aim Rotation set to {aimRotation} — beam now points at '{nearest.name}' ({best:0.00}m away). Fine-tune from here.", this);
    }

    [ContextMenu("Reset Aim Rotation")]
    private void ResetAim()
    {
        UnityEditor.Undo.RecordObject(this, "Reset Scanner Aim");
        aimRotation = Vector3.zero;
        aimOffset = Vector3.zero;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
