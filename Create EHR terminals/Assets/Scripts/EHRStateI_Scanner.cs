using UnityEngine;

public class EHRState_Scanner : EHRState
{
    public EHRState_Scanner(EHRContext context) : base(context) { }
    public override EHRStateId Id => EHRStateId.Scanner;

    public override void Enter()
    {
        ctx.ShowRedAlert3D(false);
        ctx.SetInteractables(keyboard: false, scanner: true, screen: false);

        if (ctx.ScreenText != null)
            ctx.ScreenText.text = "SCANNER STATE\nScan wristband / medication";

        Debug.Log("[EHR] Scanner entered: scanner only. Keyboard/screen disabled.");
        Debug.Log("[EHR] TODO: connect to scanner completion condition.");
    }
}