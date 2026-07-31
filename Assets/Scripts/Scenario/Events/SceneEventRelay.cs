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
    [SerializeField] private GameEvent channel;
    [SerializeField] private UnityEvent response;

    private void OnEnable()
    {
        if (channel != null)
            channel.Subscribe(Invoke);
    }

    private void OnDisable()
    {
        if (channel != null)
            channel.Unsubscribe(Invoke);
    }

    private void Invoke() => response?.Invoke();
}
