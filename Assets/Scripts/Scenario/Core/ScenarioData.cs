using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Designer-editable scenario asset: an ordered list of steps to run linearly.
/// </summary>
[CreateAssetMenu(fileName = "New Scenario", menuName = "Scenario/Scenario Data")]
public class ScenarioData : ScriptableObject
{
    [SerializeField] private List<ScenarioStepData> steps = new List<ScenarioStepData>();

    public IReadOnlyList<ScenarioStepData> Steps => steps;
}
