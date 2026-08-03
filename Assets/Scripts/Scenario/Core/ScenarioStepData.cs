using UnityEngine;

/// <summary>
/// Abstract base for every scenario step asset. Each concrete step holds only its own
/// data and produces its matching runtime executor.
/// </summary>
public abstract class ScenarioStepData : ScriptableObject
{
    public abstract IScenarioStep CreateRuntimeStep();
}
