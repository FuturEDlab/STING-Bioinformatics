using UnityEngine;

/// <summary>
/// Adapter: gameplay UnityEvent -> StringGameEvent channel.
///
/// Drop this on an interactable (e.g. the scanner), set the task id, then wire the BNG
/// Grabbable's grab event to Raise(). The scanner ends up knowing nothing about the scenario.
/// </summary>
public class TaskEventRaiser : MonoBehaviour
{
    [Tooltip("Channel to raise on. WaitForTaskStep listens on the same asset.")]
    [SerializeField] private StringGameEvent channel;

    [Tooltip("Id sent when Raise() is called, e.g. \"scanner\".")]
    [SerializeField] private string taskId = "";

    [Tooltip("Raise only the first time. Stops a repeatedly grabbed item spamming the channel.")]
    [SerializeField] private bool onlyOnce = true;

    private bool raised;

    /// <summary>Wire this to the Grabbable's grab UnityEvent.</summary>
    public void Raise()
    {
        if (onlyOnce && raised)
            return;

        if (channel == null)
        {
            Debug.LogWarning($"[Scenario] TaskEventRaiser on '{name}' has no channel assigned.", this);
            return;
        }

        raised = true;
        channel.RaiseString(taskId);
    }

    /// <summary>Raise an explicit id, for objects that report more than one task.</summary>
    public void RaiseId(string id)
    {
        if (channel == null) return;

        raised = true;
        channel.RaiseString(id);
    }

    /// <summary>Re-arms a once-only raiser, e.g. when the scenario restarts.</summary>
    public void ResetRaised()
    {
        raised = false;
    }
}
