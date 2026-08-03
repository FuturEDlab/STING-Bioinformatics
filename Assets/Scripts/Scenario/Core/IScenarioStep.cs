using System;

/// <summary>
/// Runtime executor for a single scenario step. Data (the ScriptableObject) and
/// runtime behaviour (this plain C# class) are kept deliberately separate.
/// </summary>
public interface IScenarioStep
{
    void Enter(ScenarioContext ctx, Action onComplete);
    void Exit();
}
