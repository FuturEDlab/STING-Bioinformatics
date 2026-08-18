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
    [Tooltip("Fade to black before moving and back in afterwards. The rig adds a fader on demand, so this needs no setup.")]
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
/// Runtime executor for <see cref="TeleportStepData"/>. Moves the rig directly rather than
/// going through XRI's <c>TeleportationProvider</c>, which expects a player-driven teleport
/// request from an interactor. The actual move is <see cref="PlayerRig.TeleportTo"/>, so a
/// scripted teleport lands the player exactly where walking there would.
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
        ScreenFade fader = data.FadeScreen ? ctx.Fade : null;

        if (fader != null)
        {
            fader.DoFadeIn();
            // ScreenFade has no completion callback; its alpha ramps at FadeInSpeed per second.
            yield return new WaitForSeconds(FadeSeconds(fader.FadeInSpeed) + Mathf.Max(0f, data.FadeHoldSeconds));
        }

        if (data.DelayBeforeTeleport > 0f)
            yield return new WaitForSeconds(data.DelayBeforeTeleport);

        Move(destination);

        // Let the frame finish before anything else runs, so physics settles at the new
        // position before the screen comes back.
        yield return new WaitForEndOfFrame();

        if (fader != null)
            fader.DoFadeOut();

        if (data.DelayAfterTeleport > 0f)
            yield return new WaitForSeconds(data.DelayAfterTeleport);

        routine = null;
        onComplete?.Invoke();
    }

    private void Move(Transform destination)
    {
        PlayerRig rig = ctx.Player;

        if (rig == null)
        {
            Debug.LogWarning("[TeleportStep] There is no PlayerRig in the scene and none assigned on the ScenarioController; nothing to move.");
            return;
        }

        // The rig puts the player's CAMERA on the spot rather than its origin, and handles
        // switching the character capsule off across the move. Room-scale players are metres
        // from their origin once they have walked around, so moving the origin would drop
        // them somewhere the marker never pointed at.
        rig.TeleportTo(ResolveFootPosition(destination), data.MatchDestinationRotation ? destination.forward : (Vector3?)null);
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
        if (data.FadeScreen && ctx != null && ctx.Fade != null)
            ctx.Fade.DoFadeOut();
    }

    private static float FadeSeconds(float speed) => 1f / Mathf.Max(0.01f, speed);
}
