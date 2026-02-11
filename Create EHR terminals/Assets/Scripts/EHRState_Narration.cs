using UnityEngine;

public class EHRState_Narration : EHRState
{
    public EHRState_Narration(EHRContext context) : base(context) { }
    public override EHRStateId Id => EHRStateId.Narration;

    public override void Enter()
    {
        ctx.ShowRedAlert3D(false);
        ctx.SetInteractables(keyboard: false, scanner: false, screen: false);

        if (ctx.ScreenText != null)
            ctx.ScreenText.text = "NARRATION\nNo input allowed";

        Debug.Log("[EHR] Narration entered: lock all input during nurse instructions.");
    }
}