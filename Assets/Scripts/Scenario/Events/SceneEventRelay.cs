using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Adapter: GameEvent channel -> a scene-bound UnityEvent.
///
/// A ScriptableObject's UnityEvent cannot reference scene objects (Unity limitation), so a
/// step raises a channel and this in-scene component runs the actual scene call - animator
/// triggers, timelines, enabling props, and so on.
/// </summary>
public class SceneEventRelay : MonoBehaviour
{
    [Header("Listen")]
    [Tooltip("Channel this relay listens on. A step raises it; the response below runs.")]
    [SerializeField] private GameEvent channel;

    [Tooltip("What happens in the scene when the channel is raised.")]
    [SerializeField] private UnityEvent response;

    [Header("Report back (optional)")]
    [Tooltip("Raised by ReportComplete(). Pair with 'Wait For External Completion' on InvokeSceneEventStepData.")]
    [SerializeField] private GameEvent completionChannel;

    private void OnEnable()
    {
        if (channel != null)
            channel.Subscribe(OnChannelRaised);
    }

    private void OnDisable()
    {
        if (channel != null)
            channel.Unsubscribe(OnChannelRaised);
    }

    private void OnChannelRaised()
    {
        response?.Invoke();
    }

    /// <summary>
    /// Call when the scene action has actually finished - from an Animation Event, a Timeline
    /// signal, or your own script. Lets the waiting step advance.
    /// </summary>
    public void ReportComplete()
    {
        if (completionChannel != null)
            completionChannel.Raise();
    }
}
