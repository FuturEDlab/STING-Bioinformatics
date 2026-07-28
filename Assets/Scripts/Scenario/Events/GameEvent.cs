using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parameterless Observer channel as an asset. A raiser knows nothing about its listeners,
/// and a listener knows nothing about the raiser - they only share this asset.
/// </summary>
[CreateAssetMenu(fileName = "GameEvent", menuName = "Scenario/Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private readonly List<Action> listeners = new List<Action>();

    public int ListenerCount => listeners.Count;

    public void Subscribe(Action listener)
    {
        if (listener == null || listeners.Contains(listener)) return;
        listeners.Add(listener);
    }

    public void Unsubscribe(Action listener)
    {
        listeners.Remove(listener);
    }

    /// <summary>
    /// Notify every listener. Iterates backwards so a listener that unsubscribes itself
    /// inside its own handler does not shift the list out from under us.
    /// </summary>
    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i]?.Invoke();
    }

    // ScriptableObject state survives play-mode reloads in the Editor. Clearing here stops
    // subscriptions from a previous session firing into destroyed objects.
    private void OnDisable()
    {
        listeners.Clear();
    }
}
