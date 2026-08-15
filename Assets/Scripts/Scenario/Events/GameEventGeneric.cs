using System;
using UnityEngine;

/// <summary>
/// Typed observer channel so a step can wait for a SPECIFIC payload (e.g. an item id).
/// Abstract because Unity cannot create assets from an open generic — subclass with a
/// concrete T (see StringGameEvent).
/// </summary>
public abstract class GameEvent<T> : ScriptableObject
{
    private Action<T> listeners;

    public void Subscribe(Action<T> listener) => listeners += listener;
    public void Unsubscribe(Action<T> listener) => listeners -= listener;

    public void Raise(T payload) => listeners?.Invoke(payload);

    /// <summary>How many things are listening right now. Zero means raising does nothing.</summary>
    public int ListenerCount => listeners?.GetInvocationList().Length ?? 0;

    private void OnDisable() => listeners = null;
}
