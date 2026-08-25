using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Data for a step that moves the player to a scene location, optionally fading the
/// screen around the move. The destination is a SCENE Transform, so it cannot live on
/// this asset: bind it to this step in the ScenarioController's
/// <c>Context ▸ Teleport Destinations</c> list.
/// </summary>
[CreateAssetMenu(fileName = "TeleportStep", menuName = "Scenario/Steps/Teleport")]
public class TeleportStepData : ScenarioStepData
{
    [Header("Destination (the scene Transform is bound on the ScenarioController)")]
    [Tooltip("Face the way the destination faces. Off keeps the player's current facing.")]
    [SerializeField] private bool matchDestinationRotation = true;

    [Header("Ground")]
    [Tooltip("Drop the player onto the floor beneath the destination instead of trusting its exact height, so the marker only has to be roughly placed.")]
    [SerializeField] private bool snapToGround = true;

    [Tooltip("What counts as floor. Left as Nothing it falls back to Unity's default raycast layers.")]
    [SerializeField] private LayerMask groundLayers = ~0;

    [Tooltip("How far below the destination to look for the floor.")]
    [SerializeField] private float groundSearchDistance = 25f;

    [Header("Screen fade")]
    [Tooltip("Fade to black before moving and back in afterwards. Needs a ScreenFader on the rig.")]
    [SerializeField] private bool fadeScreen = true;

    [Tooltip("Extra seconds to hold full black after the fade finishes, before the player is moved.")]
    [SerializeField] private float fadeHoldSeconds = 0.1f;

    [Header("Timing")]
    [Tooltip("Seconds to wait before moving (after the fade, if any).")]
    [SerializeField] private float delayBeforeTeleport = 0f;

    [Tooltip("Seconds to wait after arriving before the scenario advances.")]
    [SerializeField] private float delayAfterTeleport = 0f;

    public bool MatchDestinationRotation => matchDestinationRotation;
    public bool SnapToGround => snapToGround;
    public LayerMask GroundLayers => groundLayers;
    public float GroundSearchDistance => groundSearchDistance;
    public bool FadeScreen => fadeScreen;
    public float FadeHoldSeconds => fadeHoldSeconds;
    public float DelayBeforeTeleport => delayBeforeTeleport;
    public float DelayAfterTeleport => delayAfterTeleport;

    public override IScenarioStep CreateRuntimeStep() => new TeleportStep(this);
}

/// <summary>
/// Runtime executor for <see cref="TeleportStepData"/>. Both rigs ship a teleporter of their
/// own, but both are driven by player input, so this moves the rig itself — through
/// <see cref="Rig.TeleportTo"/>, which knows to stand the BNG capsule on the floor rather
/// than bury it, and to move an XR Origin by its camera rather than by its origin. The step
/// never learns which rig it is moving.
/// </summary>
public class TeleportStep : IScenarioStep
{
    private readonly TeleportStepData data;
    private ScenarioContext ctx;
    private Coroutine routine;

    public TeleportStep(TeleportStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext ctx, Action onComplete)
    {
        this.ctx = ctx;

        Transform destination = ctx.GetTeleportDestination(data);
        if (destination == null)
        {
            Debug.LogWarning($"[TeleportStep] No destination bound for '{data.name}'. Add a row for it under Context ▸ Teleport Destinations on the ScenarioController. Skipping.");
            onComplete?.Invoke();
            return;
        }

        // No runner means no coroutines: move instantly and move on.
        if (ctx.Runner == null)
        {
            Move(destination);
            onComplete?.Invoke();
            return;
        }

        routine = ctx.Runner.StartCoroutine(Run(destination, onComplete));
    }

    private IEnumerator Run(Transform destination, Action onComplete)
    {
        bool fade = data.FadeScreen && Rig.HasFade;

        if (fade)
        {
            Rig.FadeToBlack();
            // Neither fader has a completion callback; both ramp alpha at a fixed speed per
            // second, so the wait is derived from that.
            yield return new WaitForSeconds(Rig.FadeInSeconds + Mathf.Max(0f, data.FadeHoldSeconds));
        }

        if (data.DelayBeforeTeleport > 0f)
            yield return new WaitForSeconds(data.DelayBeforeTeleport);

        Move(destination);

        // Let the frame finish before anything else touches the rig, so a character
        // controller switched back on inside Rig.TeleportTo does not resolve collisions
        // against the position it was moved out of.
        yield return new WaitForEndOfFrame();

        if (fade)
            Rig.FadeFromBlack();

        if (data.DelayAfterTeleport > 0f)
            yield return new WaitForSeconds(data.DelayAfterTeleport);

        routine = null;
        onComplete?.Invoke();
    }

    private void Move(Transform destination)
    {
        Vector3 footPosition = ResolveFootPosition(destination);
        Vector3? facing = data.MatchDestinationRotation ? destination.forward : (Vector3?)null;

        // Rig handles the rest: standing the BNG capsule on the floor rather than burying
        // it, or moving the XR Origin so the CAMERA lands here — which is not the same
        // thing once the player has walked around their play space.
        if (!Rig.TeleportTo(footPosition, facing))
            Debug.LogWarning($"[TeleportStep] '{data.name}': no player rig in the scene, so there is nothing to move. Add the BNG rig or the VR Player prefab.");
    }

    /// <summary>
    /// Where the player's feet should end up. Casting down from just above the marker
    /// means a destination left floating (or sunk slightly into the floor) still lands the
    /// player on the surface underneath it.
    /// </summary>
    private Vector3 ResolveFootPosition(Transform destination)
    {
        if (!data.SnapToGround)
            return destination.position;

        // A mask of Nothing would never hit anything — treat it as "not configured".
        int mask = data.GroundLayers.value == 0 ? Physics.DefaultRaycastLayers : data.GroundLayers.value;
        const float startHeight = 1f;
        float distance = startHeight + Mathf.Max(1f, data.GroundSearchDistance);
        Vector3 origin = destination.position + (Vector3.up * startHeight);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
            return hit.point;

        Debug.LogWarning($"[TeleportStep] '{data.name}': no ground found under the destination within {distance:0.#}m. Using the destination's own height — check its Ground Layers or move the marker over the floor.");
        return destination.position;
    }

    public void Exit()
    {
        if (routine != null && ctx != null && ctx.Runner != null)
            ctx.Runner.StopCoroutine(routine);
        routine = null;

        // Never leave the screen black because the step was torn down mid-move (e.g. the
        // scenario restarted).
        if (data.FadeScreen)
            Rig.FadeFromBlack();
    }
}
