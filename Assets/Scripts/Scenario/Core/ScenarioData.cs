using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A scenario is an ordered list of step assets. Reorder by dragging in the Inspector -
/// the sequence is the data, no code changes required.
/// </summary>
[CreateAssetMenu(fileName = "New Scenario", menuName = "Scenario/Scenario")]
public class ScenarioData : ScriptableObject
{
    [Tooltip("Ordered steps. The controller walks this list top to bottom.")]
    [SerializeField] private List<ScenarioStepData> steps = new List<ScenarioStepData>();

    public int StepCount => steps.Count;

    public ScenarioStepData GetStep(int index)
    {
        if (index < 0 || index >= steps.Count) return null;
        return steps[index];
    }
}
