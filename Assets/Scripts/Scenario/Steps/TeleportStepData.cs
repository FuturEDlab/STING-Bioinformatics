using System;
using System.Collections;
using BNG;
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
    public bool FadeScreen => fadeScreen;
    public float FadeHoldSeconds => fadeHoldSeconds;
    public float DelayBeforeTeleport => delayBeforeTeleport;
    public float DelayAfterTeleport => delayAfterTeleport;

    public override IScenarioStep CreateRuntimeStep() => new TeleportStep(this);
}

/// <summary>
/// Runtime executor for <see cref="TeleportStepData"/>. Moves the rig directly rather
/// than going through BNG's <c>PlayerTeleport</c> (which is driven by player input), but
/// mirrors that component's CharacterController handling and height offset so the player
/// lands exactly where a normal BNG teleport would put them.
/// </summary>
public class TeleportStep : IScenarioStep
{
    private readonly TeleportStepData data;
    private ScenarioContext ctx;
    private Coroutine routine;
    private CharacterController disabledController;

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
            RestoreController();
            onComplete?.Invoke();
            return;
        }

        routine = ctx.Runner.StartCoroutine(Run(destination, onComplete));
    }

    private IEnumerator Run(Transform destination, Action onComplete)
    {
        ScreenFader fader = data.FadeScreen ? ctx.ScreenFader : null;

        if (fader != null)
        {
            fader.DoFadeIn();
            // ScreenFader has no completion callback; its alpha ramps at FadeInSpeed per second.
            yield return new WaitForSeconds(FadeSeconds(fader.FadeInSpeed) + Mathf.Max(0f, data.FadeHoldSeconds));
        }

        if (data.DelayBeforeTeleport > 0f)
            yield return new WaitForSeconds(data.DelayBeforeTeleport);

        Move(destination);

        // Let the frame finish before the CharacterController is switched back on, so it
        // doesn't resolve collisions against the position it was moved out of.
        yield return new WaitForEndOfFrame();
        RestoreController();

        if (fader != null)
            fader.DoFadeOut();

        if (data.DelayAfterTeleport > 0f)
            yield return new WaitForSeconds(data.DelayAfterTeleport);

        routine = null;
        onComplete?.Invoke();
    }

    private void Move(Transform destination)
    {
        BNGPlayerController player = ctx.Player;
        Transform rig = ctx.PlayerRig;
        Transform root = player != null ? player.transform : rig;

        if (root == null)
        {
            Debug.LogWarning("[TeleportStep] Context has no player or playerRig assigned; nothing to move.");
            return;
        }

        CharacterController controller = root.GetComponentInChildren<CharacterController>();
        Transform target = controller != null ? controller.transform : (rig != null ? rig : root);

        // A CharacterController overrides direct transform writes, so switch it off first.
        if (controller != null)
        {
            controller.enabled = false;
            disabledController = controller;
        }

        target.position = destination.position;

        // BNG offsets the character by the tracked camera height; apply the same correction
        // so the player lands on the floor rather than floating above it.
        if (controller != null && player != null && player.CameraRig != null)
        {
            float yOffset = 1f + player.CameraRig.localPosition.y - player.CharacterControllerYOffset;
            target.localPosition -= new Vector3(0f, yOffset, 0f);
        }

        if (data.MatchDestinationRotation)
        {
            target.rotation = destination.rotation;
            // Keep the player upright however the destination marker is tilted.
            target.eulerAngles = new Vector3(0f, target.eulerAngles.y, 0f);
        }

        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body != null)
            body.linearVelocity = Vector3.zero;

        if (player != null)
            player.LastTeleportTime = Time.time;
    }

    public void Exit()
    {
        if (routine != null && ctx != null && ctx.Runner != null)
            ctx.Runner.StopCoroutine(routine);
        routine = null;

        // Never leave the player frozen or the screen black because the step was torn
        // down mid-move (e.g. the scenario restarted).
        RestoreController();
        if (data.FadeScreen && ctx != null && ctx.ScreenFader != null)
            ctx.ScreenFader.DoFadeOut();
    }

    private void RestoreController()
    {
        if (disabledController != null)
        {
            disabledController.enabled = true;
            disabledController = null;
        }
    }

    private static float FadeSeconds(float speed) => 1f / Mathf.Max(0.01f, speed);
}
