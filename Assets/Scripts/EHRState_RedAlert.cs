using UnityEngine;

public class EHRState_RedAlert : EHRState
{
    public EHRState_RedAlert(EHRContext context) : base(context) { }
    public override EHRStateId Id => EHRStateId.RedAlert;

    public override void Enter()
    {
        ctx.ShowRedAlert3D(true);
        ctx.SetInteractables(keyboard: false, scanner: false, screen: false);

        if (ctx.ScreenText != null)
            ctx.ScreenText.text = "RED ALERT!\nInput error detected";

        Debug.Log("[EHR] RedAlert entered: show red alert 3D + lock all input.");
    }

    public override void Exit()
    {
        ctx.ShowRedAlert3D(false);
    }
}