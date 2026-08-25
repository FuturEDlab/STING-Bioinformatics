using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controller buttons, for gameplay scripts that just want to know "is the trigger down?".
///
/// This replaces the handful of <c>InputBridge.Instance</c> reads the project used to make.
/// The bindings are written in code against the generic <c>XRController</c> layout with a
/// LeftHand/RightHand usage, so the same call works for Quest controllers in a headset and
/// for the XR Interaction Simulator's virtual controllers on the desktop — no Inspector
/// wiring, and nothing to keep in sync with the rig prefab.
///
/// Interactor-driven behaviour (grabbing, teleporting, UI rays) does <em>not</em> come
/// through here: XRI's interactors read their own actions off the rig. This is only for the
/// few places gameplay needs a raw button.
/// </summary>
public static class XRInputRouter
{
    private const string LeftController = "<XRController>{LeftHand}";
    private const string RightController = "<XRController>{RightHand}";

    private enum Control
    {
        LeftTrigger,
        RightTrigger,
        LeftGrip,
        RightGrip,
        LeftPrimary,
        RightPrimary,
        LeftSecondary,
        RightSecondary,
        Count,
    }

    private static readonly InputAction[] Actions = new InputAction[(int)Control.Count];
    private static bool ready;

    /// <summary>Left trigger pull, 0-1.</summary>
    public static float LeftTrigger => Value(Control.LeftTrigger);

    /// <summary>Right trigger pull, 0-1.</summary>
    public static float RightTrigger => Value(Control.RightTrigger);

    /// <summary>Left trigger crossed the press threshold this frame.</summary>
    public static bool LeftTriggerDown => Pressed(Control.LeftTrigger);

    /// <summary>Right trigger crossed the press threshold this frame.</summary>
    public static bool RightTriggerDown => Pressed(Control.RightTrigger);

    /// <summary>Left grip squeeze, 0-1.</summary>
    public static float LeftGrip => Value(Control.LeftGrip);

    /// <summary>Right grip squeeze, 0-1.</summary>
    public static float RightGrip => Value(Control.RightGrip);

    /// <summary>Left grip crossed the press threshold this frame.</summary>
    public static bool LeftGripDown => Pressed(Control.LeftGrip);

    /// <summary>Right grip crossed the press threshold this frame.</summary>
    public static bool RightGripDown => Pressed(Control.RightGrip);

    /// <summary>X on a Touch controller — the left hand's lower face button.</summary>
    public static bool XButton => Held(Control.LeftPrimary);

    /// <summary>X pressed this frame.</summary>
    public static bool XButtonDown => Pressed(Control.LeftPrimary);

    /// <summary>Y on a Touch controller — the left hand's upper face button.</summary>
    public static bool YButton => Held(Control.LeftSecondary);

    /// <summary>Y pressed this frame.</summary>
    public static bool YButtonDown => Pressed(Control.LeftSecondary);

    /// <summary>A on a Touch controller — the right hand's lower face button.</summary>
    public static bool AButton => Held(Control.RightPrimary);

    /// <summary>A pressed this frame.</summary>
    public static bool AButtonDown => Pressed(Control.RightPrimary);

    /// <summary>B on a Touch controller — the right hand's upper face button.</summary>
    public static bool BButton => Held(Control.RightSecondary);

    /// <summary>B pressed this frame.</summary>
    public static bool BButtonDown => Pressed(Control.RightSecondary);

    /// <summary>
    /// Statics survive play-mode entry when domain reload is off, so the actions have to be
    /// thrown away deliberately or the second run reads from disposed ones.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActions()
    {
        for (int i = 0; i < Actions.Length; i++)
        {
            Actions[i]?.Dispose();
            Actions[i] = null;
        }

        ready = false;
    }

    private static InputAction Get(Control control)
    {
        if (!ready)
        {
            ready = true;

            Actions[(int)Control.LeftTrigger] = Axis("XRI Adapter/Left Trigger", LeftController + "/trigger");
            Actions[(int)Control.RightTrigger] = Axis("XRI Adapter/Right Trigger", RightController + "/trigger");
            Actions[(int)Control.LeftGrip] = Axis("XRI Adapter/Left Grip", LeftController + "/grip");
            Actions[(int)Control.RightGrip] = Axis("XRI Adapter/Right Grip", RightController + "/grip");
            Actions[(int)Control.LeftPrimary] = Button("XRI Adapter/Left Primary", LeftController + "/primaryButton");
            Actions[(int)Control.RightPrimary] = Button("XRI Adapter/Right Primary", RightController + "/primaryButton");
            Actions[(int)Control.LeftSecondary] = Button("XRI Adapter/Left Secondary", LeftController + "/secondaryButton");
            Actions[(int)Control.RightSecondary] = Button("XRI Adapter/Right Secondary", RightController + "/secondaryButton");
        }

        return Actions[(int)control];
    }

    /// <summary>
    /// An analog control read as a value and as a button. The press point comes from the
    /// project's Input System settings, so trigger and grip feel the same here as they do to
    /// XRI's own interactors.
    /// </summary>
    private static InputAction Axis(string name, string binding)
    {
        InputAction action = new InputAction(name, InputActionType.Value, binding, expectedControlType: "Axis");
        action.Enable();
        return action;
    }

    private static InputAction Button(string name, string binding)
    {
        InputAction action = new InputAction(name, InputActionType.Button, binding);
        action.Enable();
        return action;
    }

    private static float Value(Control control)
    {
        InputAction action = Get(control);
        return action != null ? action.ReadValue<float>() : 0f;
    }

    private static bool Held(Control control)
    {
        InputAction action = Get(control);
        return action != null && action.IsPressed();
    }

    private static bool Pressed(Control control)
    {
        InputAction action = Get(control);
        return action != null && action.WasPressedThisFrame();
    }
}
