using System.Collections.Generic;
using UnityEngine;

public class EHRActionCycler : MonoBehaviour
{
    [Tooltip("Reference to the EHRManager that will receive action calls")]
    public EHRManager ehrManager;

    [Tooltip("List of action names to send in order when the button is clicked")]
    public List<string> actions = new List<string>();

    int index = 0;

    // Hook this parameterless method to Button.onClick
    public void OnClickCycle()
    {
        if (ehrManager == null || actions == null || actions.Count == 0) return;

        // Clamp index to valid range (if already at end, stay at last)
        index = Mathf.Clamp(index, 0, actions.Count - 1);

        string action = actions[index];
        ehrManager.AdvanceByAction(action);

        // Advance index but do not loop; stay at last when reached
        if (index < actions.Count - 1) index++;
    }

    public void ResetIndex() => index = 0;
    public void SetIndex(int i) => index = Mathf.Clamp(i, 0, Mathf.Max(0, actions.Count - 1));
}
