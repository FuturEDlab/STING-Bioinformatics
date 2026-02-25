using UnityEngine;

public class EHRState_SleepIdle : EHRState
{
    public EHRState_SleepIdle(EHRContext context) : base(context) { }
    public override EHRStateId Id => EHRStateId.SleepIdle;

    public override void Enter()
    {
        ctx.ShowRedAlert3D(false);
        ctx.SetInteractables(keyboard: false, scanner: false, screen: false);

        if (ctx.ScreenText != null)
            ctx.ScreenText.text = "GVSU EHR\nSLEEP MODE";

        Debug.Log("[EHR] Sleep/Idle entered: show logo, no input allowed.");
    }
}