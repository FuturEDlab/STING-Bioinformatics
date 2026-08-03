using System;
using UnityEngine;

/// <summary>
/// Parameterless observer channel. The subject: gameplay/UI code calls Raise();
/// scenario steps Subscribe/Unsubscribe as observers.
/// </summary>
[CreateAssetMenu(fileName = "New Game Event", menuName = "Scenario/Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private Action listeners;

    public void Subscribe(Action listener) => listeners += listener;
    public void Unsubscribe(Action listener) => listeners -= listener;

    public void Raise() => listeners?.Invoke();

    // Clear any stale subscriptions when the asset unloads (domain/play-mode reload).
    private void OnDisable() => listeners = null;
}
