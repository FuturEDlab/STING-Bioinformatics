using UnityEngine;

public class EHRState_Override : EHRState
{
    public EHRState_Override(EHRContext context) : base(context) { }
    public override EHRStateId Id => EHRStateId.Override;

    public override void Enter()
    {
        ctx.ShowRedAlert3D(false);
        ctx.SetInteractables(keyboard: true, scanner: false, screen: false);

        if (ctx.ScreenText != null)
            ctx.ScreenText.text = "OVERRIDE REQUIRED\nKeyboard ONLY";

        Debug.Log("[EHR] Override entered: keyboard only. Scanner/screen disabled.");
    }
}