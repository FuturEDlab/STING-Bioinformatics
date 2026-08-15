using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bridges a parameterless GameEvent channel to scene-bound logic. Needed because a
/// UnityEvent serialized on a ScriptableObject asset cannot reference scene objects —
/// so a scenario step raises the channel and this scene component fires the response,
/// whose UnityEvent CAN target scene methods.
/// </summary>
public class SceneEventRelay : MonoBehaviour
{
    [Tooltip("The scenario channel to listen on, e.g. EV_EHR_MethoAlert.")]
    [SerializeField] private GameEvent channel;

    [Tooltip("What should happen in the scene when that channel is raised.")]
    [SerializeField] private UnityEvent response;

    [Tooltip("Log to the console every time this relay fires. Useful while wiring the EHR up.")]
    [SerializeField] private bool debugLogging;

    /// <summary>The channel this relay listens on. Read by the controller's wiring report.</summary>
    public GameEvent Channel => channel;

    // NOTE: subscription happens in OnEnable, so a relay sitting on a DEACTIVATED
    // GameObject is not listening at all. Keep relays on an object that is always on —
    // never on the panel or prop they switch on, or they can never hear the event that
    // was supposed to switch it on.
    private void OnEnable()
    {
        if (channel != null)
            channel.Subscribe(Invoke);
        else
            Debug.LogWarning($"[SceneEventRelay] '{name}' has no channel assigned; it will never fire.", this);
    }

    private void OnDisable()
    {
        if (channel != null)
            channel.Unsubscribe(Invoke);
    }

    private void Invoke()
    {
        if (debugLogging)
            Debug.Log($"[SceneEventRelay] '{name}' fired on '{channel.name}'.", this);

        response?.Invoke();
    }
}
