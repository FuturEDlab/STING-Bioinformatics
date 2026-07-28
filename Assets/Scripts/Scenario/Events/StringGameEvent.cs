using UnityEngine;

/// <summary>
/// Typed channel carrying a task or item id, e.g. "scanner", "patient-notes".
/// This is the channel WaitForTaskStep listens on.
/// </summary>
[CreateAssetMenu(fileName = "StringGameEvent", menuName = "Scenario/Events/String Game Event")]
public class StringGameEvent : GameEvent<string>
{
    /// <summary>
    /// UnityEvent-friendly entry point. Bind this directly to a BNG Grabbable's grab event
    /// (or any UnityEvent&lt;string&gt;-capable slot) and type the id in the Inspector.
    /// </summary>
    public void RaiseString(string value)
    {
        Raise(value);
    }
}
