using UnityEngine;

public class EHRState_PatientNotes : EHRState
{
    public EHRState_PatientNotes(EHRContext context) : base(context) { }
    public override EHRStateId Id => EHRStateId.PatientNotes;

    public override void Enter()
    {
        ctx.ShowRedAlert3D(false);

        // Scanner not interactable; screen shows notes; keyboard locked initially
        ctx.SetInteractables(keyboard: false, scanner: false, screen: true);

        if (ctx.ScreenText != null)
            ctx.ScreenText.text = "PATIENT NOTES\nLast dose: 500mg\nEnter current dose";

        Debug.Log("[EHR] PatientNotes entered: scanner disabled. Screen shows dosage + input box.");
        Debug.Log("[EHR] TODO: show numeric keypad when textbox selected.");
        Debug.Log("[EHR] TODO: enable keyboard only after at least one number entered.");
    }
}