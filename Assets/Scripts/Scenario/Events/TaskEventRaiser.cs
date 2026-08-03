using UnityEngine;

/// <summary>
/// Convenience raiser for gameplay. Wire Raise() to a BNG Grabbable UnityEvent
/// (onRelease / onSnapZoneEnter) or the project's Interact.onInteract; it raises the
/// typed task channel with its id, with zero knowledge of the scenario.
/// </summary>
public class TaskEventRaiser : MonoBehaviour
{
    [SerializeField] private StringGameEvent channel;
    [SerializeField] private string taskId;

    [ContextMenu("Raise")]
    public void Raise()
    {
        if (channel != null)
            channel.Raise(taskId);
    }
}
