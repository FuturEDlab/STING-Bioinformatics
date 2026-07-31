using UnityEngine;

/// <summary>
/// Typed GameEvent variant carrying a string id (e.g. a task/item id).
/// </summary>
[CreateAssetMenu(fileName = "New String Game Event", menuName = "Scenario/Events/String Game Event")]
public class StringGameEvent : GameEvent<string>
{
    /// <summary>Convenience for scene UnityEvents, which can bind a one-arg string method.</summary>
    public void RaiseString(string id) => Raise(id);
}
