using System;
using UnityEngine;

/// <summary>Plays a narration clip and completes when it ends.</summary>
[CreateAssetMenu(fileName = "NarratorStep", menuName = "Scenario/Steps/Narrator")]
public class NarratorStepData : ScenarioStepData
{
    [Tooltip("Voice over clip. Plays through ScenarioContext.PlayVoice, so it routes to the Voices mixer group.")]
    public AudioClip clip;

    [TextArea(2, 5)]
    [Tooltip("Script text, for authoring reference and future captions. Not displayed yet.")]
    public string transcript;

    public override IScenarioStep CreateRuntimeStep() => new NarratorStep(this);
}

public class NarratorStep : IScenarioStep
{
    private readonly NarratorStepData data;
    private ScenarioContext ctx;

    public NarratorStep(NarratorStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext context, Action onComplete)
    {
        ctx = context;

        // PlayVoice invokes the callback when the clip ends - or immediately if there is
        // no clip, so a missing asset cannot stall the scenario.
        ctx.PlayVoice(data.clip, onComplete);
    }

    public void Exit()
    {
        ctx?.StopVoice();
        ctx = null;
    }
}
