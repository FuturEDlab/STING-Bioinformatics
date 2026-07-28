using UnityEngine;

/// <summary>
/// The data half of a scenario step: a designer-authored asset holding only that step's
/// configuration. Factory Method - each subclass decides which runtime class to build,
/// so ScenarioController never switches on step type.
/// </summary>
public abstract class ScenarioStepData : ScriptableObject
{
    [TextArea(1, 3)]
    [Tooltip("Authoring note. Not used at runtime.")]
    public string designerNote;

    public abstract IScenarioStep CreateRuntimeStep();
}
