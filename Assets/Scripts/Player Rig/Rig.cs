using UnityEngine;

/// <summary>
/// The single place gameplay asks "where is the player, and what are they pressing?".
///
/// The project ships two player rigs and both have to keep working:
///
///  * <b>BNG "XR Rig Full Body"</b> — the original army-guy rig, still used by
///    <c>Assets/Scenes/Hospital Room.unity</c>. Head, hands, capsule and buttons all come
///    off <c>InputBridge</c> / <c>BNGPlayerController</c>.
///  * <b>"VR Player"</b> — the new XRI hands in <c>Assets/Prefabs/Player/</c>, used by
///    <c>Mohamed Test Scene</c>. Head, hands and capsule come off <see cref="PlayerRig"/>;
///    buttons come off <see cref="XRInputRouter"/>.
///
/// Gameplay scripts used to name one of those directly, which is what tied every one of
/// them to a single prefab. They now come through here instead, so the same
/// <c>Interact</c>, <c>Draggable</c>, <c>ScannerTool</c> and scenario steps run unchanged in
/// either scene, and a third rig later means editing this file rather than fifteen others.
///
/// <b>Which rig is live</b> is decided by one question: is there a <see cref="PlayerRig"/>
/// in the loaded scene? If yes, the XRI path is used and BNG is never touched. If no, the
/// BNG path is used. Nothing has to be configured for that to happen.
/// </summary>
public static class Rig
{
    // Both lookups are cached, and a miss is retried at most once per frame. Without that
    // guard a BNG scene would run a scene-wide search for a PlayerRig on every property
    // read, every frame, from every prop in the room.
    private static PlayerRig xrRig;
    private static BNG.InputBridge bngInput;
    private static BNG.BNGPlayerController bngPlayer;
    private static int xrSearchedFrame = -1;
    private static int bngSearchedFrame = -1;

    /// <summary>
    /// Statics outlive play mode when domain reload is off, so a stale rig from the last
    /// run has to be dropped deliberately.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        xrRig = null;
        bngInput = null;
        bngPlayer = null;
        xrSearchedFrame = -1;
        bngSearchedFrame = -1;
    }

    /// <summary>
    /// Call after loading a scene, or after spawning a rig, if something needs the very
    /// next read to see it rather than waiting a frame. Rarely needed.
    /// </summary>
    public static void Invalidate() => Reset();

    // ------------------------------------------------------------------ which rig is live

    /// <summary>The new XRI hands rig, or null when this scene runs the BNG one.</summary>
    public static PlayerRig XR
    {
        get
        {
            if (xrRig != null)
                return xrRig;

            if (xrSearchedFrame == Time.frameCount)
                return null;

            xrSearchedFrame = Time.frameCount;
            xrRig = Object.FindAnyObjectByType<PlayerRig>(FindObjectsInactive.Exclude);
            return xrRig;
        }
    }

    /// <summary>True in a scene running the new hands.</summary>
    public static bool UsingXRHands => XR != null;

    /// <summary>
    /// BNG's input hub, or null. Deliberately not <c>InputBridge.Instance</c>: that getter
    /// spawns an InputBridge GameObject when it cannot find one, so asking it "is BNG here?"
    /// in an XRI scene would answer by creating the thing it was asked about.
    /// </summary>
    private static BNG.InputBridge Bng
    {
        get
        {
            if (UsingXRHands)
                return null;

            if (bngInput != null)
                return bngInput;

            if (bngSearchedFrame == Time.frameCount)
                return null;

            bngSearchedFrame = Time.frameCount;
            bngInput = Object.FindAnyObjectByType<BNG.InputBridge>(FindObjectsInactive.Exclude);
            return bngInput;
        }
    }

    /// <summary>The BNG player controller, or null in an XRI scene.</summary>
    public static BNG.BNGPlayerController BngPlayer
    {
        get
        {
            if (UsingXRHands)
                return null;

            if (bngPlayer != null)
                return bngPlayer;

            BNG.InputBridge input = Bng;
            if (input != null)
                bngPlayer = input.GetComponentInChildren<BNG.BNGPlayerController>();

            if (bngPlayer == null)
                bngPlayer = Object.FindAnyObjectByType<BNG.BNGPlayerController>(FindObjectsInactive.Exclude);

            return bngPlayer;
        }
    }

    /// <summary>True when neither rig is in the scene — everything below reads as empty.</summary>
    public static bool Missing => XR == null && BngPlayer == null && Bng == null;

    // ------------------------------------------------------------------------- transforms

    /// <summary>
    /// Where the player is standing, at floor level — the character capsule. This is what
    /// proximity checks ("is the player close enough to this cabinet?") should measure
    /// against, not the head.
    /// </summary>
    public static Transform Body
    {
        get
        {
            PlayerRig rig = XR;
            if (rig != null)
                return rig.Body;

            BNG.BNGPlayerController player = BngPlayer;
            return player != null ? player.transform : null;
        }
    }

    /// <summary>The head/camera. Use for anything that follows where the player is looking.</summary>
    public static Transform Head
    {
        get
        {
            PlayerRig rig = XR;
            if (rig != null && rig.Head != null)
                return rig.Head;

            BNG.BNGPlayerController player = BngPlayer;
            if (player != null && player.CameraRig != null)
            {
                Camera cam = player.CameraRig.GetComponentInChildren<Camera>(true);
                if (cam != null)
                    return cam.transform;
            }

            // Last resort. Camera.main is right far more often than it is wrong here, and a
            // panel that appears in a slightly odd place beats one that never appears.
            Camera main = Camera.main;
            return main != null ? main.transform : null;
        }
    }

    /// <summary>
    /// The left hand. On the XRI rig this is the tracked hand object; on BNG it is the
    /// LeftController. Either way it points the way the hand points, which is what the
    /// keyboard and TV rays cast along.
    /// </summary>
    public static Transform LeftHand
    {
        get
        {
            PlayerRig rig = XR;
            return rig != null ? rig.LeftHand : BngHand(BNG.ControllerHand.Left);
        }
    }

    /// <summary>The right hand. See <see cref="LeftHand"/>.</summary>
    public static Transform RightHand
    {
        get
        {
            PlayerRig rig = XR;
            return rig != null ? rig.RightHand : BngHand(BNG.ControllerHand.Right);
        }
    }

    /// <summary>
    /// The BNG rig's hand transform for one side. Handedness lives on <c>Grabber</c>, not on
    /// <c>HandController</c>, so the Grabber is what gets asked — then walk up to the
    /// controller object it hangs off, which is the transform whose forward the keyboard and
    /// TV rays are cast along.
    /// </summary>
    private static Transform BngHand(BNG.ControllerHand side)
    {
        BNG.InputBridge input = Bng;
        if (input == null)
            return null;

        BNG.Grabber[] grabbers = input.GetComponentsInChildren<BNG.Grabber>(true);
        for (int i = 0; i < grabbers.Length; i++)
        {
            if (grabbers[i].HandSide != side)
                continue;

            BNG.HandController hand = grabbers[i].GetComponentInParent<BNG.HandController>();
            if (hand != null)
                return hand.transform;

            return grabbers[i].transform.parent != null ? grabbers[i].transform.parent : grabbers[i].transform;
        }

        return null;
    }

    /// <summary>
    /// The collider props should be told to ignore, so a held bottle cannot shove the
    /// player across the room.
    /// </summary>
    public static Collider BodyCollider
    {
        get
        {
            PlayerRig rig = XR;
            if (rig != null)
                return rig.BodyCollider;

            BNG.BNGPlayerController player = BngPlayer;
            return player != null ? player.GetComponentInChildren<CharacterController>(true) : null;
        }
    }

    // ------------------------------------------------------------------------------ input
    // Analog controls are 0-1; the *Down properties are true only on the frame the control
    // crossed its press threshold. Names follow the Touch controller because that is what
    // the project is built for: X/Y on the left hand, A/B on the right.

    public static float LeftTrigger => UsingXRHands ? XRInputRouter.LeftTrigger : (Bng != null ? Bng.LeftTrigger : 0f);
    public static float RightTrigger => UsingXRHands ? XRInputRouter.RightTrigger : (Bng != null ? Bng.RightTrigger : 0f);
    public static float LeftGrip => UsingXRHands ? XRInputRouter.LeftGrip : (Bng != null ? Bng.LeftGrip : 0f);
    public static float RightGrip => UsingXRHands ? XRInputRouter.RightGrip : (Bng != null ? Bng.RightGrip : 0f);

    public static bool LeftTriggerDown => UsingXRHands ? XRInputRouter.LeftTriggerDown : (Bng != null && Bng.LeftTriggerDown);
    public static bool RightTriggerDown => UsingXRHands ? XRInputRouter.RightTriggerDown : (Bng != null && Bng.RightTriggerDown);
    public static bool LeftGripDown => UsingXRHands ? XRInputRouter.LeftGripDown : (Bng != null && Bng.LeftGripDown);
    public static bool RightGripDown => UsingXRHands ? XRInputRouter.RightGripDown : (Bng != null && Bng.RightGripDown);

    public static bool XButton => UsingXRHands ? XRInputRouter.XButton : (Bng != null && Bng.XButton);
    public static bool XButtonDown => UsingXRHands ? XRInputRouter.XButtonDown : (Bng != null && Bng.XButtonDown);
    public static bool YButton => UsingXRHands ? XRInputRouter.YButton : (Bng != null && Bng.YButton);
    public static bool YButtonDown => UsingXRHands ? XRInputRouter.YButtonDown : (Bng != null && Bng.YButtonDown);
    public static bool AButton => UsingXRHands ? XRInputRouter.AButton : (Bng != null && Bng.AButton);
    public static bool AButtonDown => UsingXRHands ? XRInputRouter.AButtonDown : (Bng != null && Bng.AButtonDown);
    public static bool BButton => UsingXRHands ? XRInputRouter.BButton : (Bng != null && Bng.BButton);
    public static bool BButtonDown => UsingXRHands ? XRInputRouter.BButtonDown : (Bng != null && Bng.BButtonDown);

    // ------------------------------------------------------------------------------- fade

    /// <summary>Fade the view to black. Does nothing when the rig has no fader.</summary>
    public static void FadeToBlack()
    {
        if (UsingXRHands)
        {
            ScreenFade fade = XR.Fade;
            if (fade != null)
                fade.DoFadeIn();
            return;
        }

        BNG.ScreenFader fader = BngFader;
        if (fader != null)
            fader.DoFadeIn();
    }

    /// <summary>Fade the view back to the scene.</summary>
    public static void FadeFromBlack()
    {
        if (UsingXRHands)
        {
            ScreenFade fade = XR.Fade;
            if (fade != null)
                fade.DoFadeOut();
            return;
        }

        BNG.ScreenFader fader = BngFader;
        if (fader != null)
            fader.DoFadeOut();
    }

    /// <summary>
    /// Seconds a fade to black takes on whichever rig is live, so a step that fades can
    /// wait exactly as long as the fade runs. Zero when there is no fader — the caller
    /// should skip its wait rather than hold a black screen that never lifts.
    /// </summary>
    public static float FadeInSeconds
    {
        get
        {
            if (UsingXRHands)
            {
                ScreenFade fade = XR.Fade;
                return fade != null ? 1f / Mathf.Max(0.01f, fade.FadeInSpeed) : 0f;
            }

            BNG.ScreenFader fader = BngFader;
            return fader != null ? 1f / Mathf.Max(0.01f, fader.FadeInSpeed) : 0f;
        }
    }

    /// <summary>
    /// Seconds the fade back to the scene takes. Counterpart of <see cref="FadeInSeconds"/>,
    /// for anything that has to hold until the player can see again.
    /// </summary>
    public static float FadeOutSeconds
    {
        get
        {
            if (UsingXRHands)
            {
                ScreenFade fade = XR.Fade;
                return fade != null ? 1f / Mathf.Max(0.01f, fade.FadeOutSpeed) : 0f;
            }

            BNG.ScreenFader fader = BngFader;
            return fader != null ? 1f / Mathf.Max(0.01f, fader.FadeOutSpeed) : 0f;
        }
    }

    /// <summary>True when a fade would actually be visible.</summary>
    public static bool HasFade => UsingXRHands ? XR.Fade != null : BngFader != null;

    /// <summary>
    /// How black the view is right now, 0-1. For anything that has to change the world
    /// behind a blackout and must not do it while the player can still see. Reads 0 when
    /// there is no fader, so a caller that waits for black gives up rather than acting.
    /// </summary>
    public static float FadeAlpha
    {
        get
        {
            if (UsingXRHands)
            {
                ScreenFade fade = XR.Fade;
                return fade != null ? fade.CurrentAlpha : 0f;
            }

            BNG.ScreenFader fader = BngFader;
            if (fader == null)
                return 0f;

            // BNG keeps its alpha on a CanvasGroup it builds at runtime and never exposes,
            // so this is a best effort — a miss reads as "not black", which is the safe answer.
            CanvasGroup group = fader.GetComponentInChildren<CanvasGroup>(true);
            return group != null ? group.alpha : 0f;
        }
    }

    /// <summary>
    /// Hold a line of text on the black — the "30 minutes later" title card. Only the new
    /// rig's <see cref="ScreenFade"/> can do this; BNG's fader is a bare quad, so on that rig
    /// the fade still happens and the card is simply skipped.
    /// </summary>
    public static void ShowFadeMessage(string message)
    {
        if (!UsingXRHands)
            return;

        ScreenFade fade = XR.Fade;
        if (fade != null)
            fade.ShowMessage(message);
    }

    /// <summary>Drop the title card. The black itself is unaffected.</summary>
    public static void HideFadeMessage()
    {
        if (!UsingXRHands)
            return;

        ScreenFade fade = XR.Fade;
        if (fade != null)
            fade.HideMessage();
    }

    private static BNG.ScreenFader BngFader
    {
        get
        {
            BNG.BNGPlayerController player = BngPlayer;
            if (player == null || player.CameraRig == null)
                return null;

            return player.CameraRig.GetComponentInChildren<BNG.ScreenFader>(true);
        }
    }

    // --------------------------------------------------------------------------- teleport

    /// <summary>
    /// Stand the player's feet on <paramref name="footPosition"/>, optionally turning them
    /// to face <paramref name="facing"/>. Returns false when there is no rig to move.
    ///
    /// On the XRI rig this moves the <em>camera</em> to the spot rather than the origin —
    /// with room scale those are metres apart once the player has walked around their play
    /// space, and moving the origin would drop them somewhere they never asked to go.
    /// </summary>
    public static bool TeleportTo(Vector3 footPosition, Vector3? facing)
    {
        PlayerRig rig = XR;
        if (rig != null)
        {
            rig.TeleportTo(footPosition, facing);
            return true;
        }

        BNG.BNGPlayerController player = BngPlayer;
        if (player == null)
            return false;

        // BNG's capsule sits at the middle of the character controller, so put the
        // transform half a capsule above the floor or the player is buried in it. Read the
        // live capsule: BNG resizes it every frame to match the tracked head height.
        CharacterController controller = player.GetComponentInChildren<CharacterController>(true);
        Transform target = controller != null ? controller.transform : player.transform;

        bool hadController = controller != null && controller.enabled;
        if (hadController)
            controller.enabled = false;

        Vector3 position = footPosition;
        if (controller != null)
            position += Vector3.up * ((controller.height * 0.5f) - controller.center.y);

        target.position = position;

        if (facing.HasValue)
        {
            Vector3 flat = Vector3.ProjectOnPlane(facing.Value, Vector3.up);
            if (flat.sqrMagnitude > 0.0001f)
                target.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
        }

        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body != null)
            body.linearVelocity = Vector3.zero;

        if (hadController)
            controller.enabled = true;

        player.LastTeleportTime = Time.time;
        return true;
    }
}
