using System;
using UnityEngine;

/// <summary>Data for a step that plays a VO clip and completes when the clip finishes.</summary>
[CreateAssetMenu(fileName = "NarratorStep", menuName = "Scenario/Steps/Narrator")]
public class NarratorStepData : ScenarioStepData
{
    [SerializeField] private AudioClip clip;

    public AudioClip Clip => clip;

    public override IScenarioStep CreateRuntimeStep() => new NarratorStep(this);
}

/// <summary>Runtime executor for <see cref="NarratorStepData"/>.</summary>
public class NarratorStep : IScenarioStep
{
    private readonly NarratorStepData data;
    private ScenarioContext ctx;

    public NarratorStep(NarratorStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext ctx, Action onComplete)
    {
        this.ctx = ctx;
        // Plays through the shared audio path; a null clip completes immediately.
        ctx.PlayVoice(data.Clip, onComplete);
    }

    public void Exit()
    {
        ctx?.StopVoice();
    }
}
