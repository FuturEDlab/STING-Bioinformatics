using System;

/// <summary>
/// The behaviour half of a scenario step. One instance is built per playthrough by
/// <see cref="ScenarioStepData.CreateRuntimeStep"/>, so a step asset can be reused freely.
///
/// Contract: Enter is called exactly once, then onComplete must be invoked exactly once
/// (immediately, or later from a callback / event). The controller calls Exit right after.
/// </summary>
public interface IScenarioStep
{
    void Enter(ScenarioContext context, Action onComplete);
    void Exit();
}
