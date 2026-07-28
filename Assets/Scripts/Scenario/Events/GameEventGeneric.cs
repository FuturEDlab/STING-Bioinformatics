using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Typed Observer channel. Abstract because Unity cannot serialize an open generic
/// ScriptableObject - subclass it with a concrete T (see <see cref="StringGameEvent"/>).
/// </summary>
public abstract class GameEvent<T> : ScriptableObject
{
    private readonly List<Action<T>> listeners = new List<Action<T>>();

    public int ListenerCount => listeners.Count;

    public void Subscribe(Action<T> listener)
    {
        if (listener == null || listeners.Contains(listener)) return;
        listeners.Add(listener);
    }

    public void Unsubscribe(Action<T> listener)
    {
        listeners.Remove(listener);
    }

    /// <summary>Notify every listener. Backwards iteration keeps self-unsubscribe safe.</summary>
    public void Raise(T value)
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i]?.Invoke(value);
    }

    protected virtual void OnDisable()
    {
        listeners.Clear();
    }
}
