using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data for a step that speaks a line and completes when it finishes. The line is a list
/// because recordings are authored as short phrases; they play back to back in order,
/// each showing its caption while it plays.
/// </summary>
[CreateAssetMenu(fileName = "NarratorStep", menuName = "Scenario/Steps/Narrator")]
public class NarratorStepData : ScenarioStepData
{
    [Tooltip("Who is speaking — cosmetic label for authoring/debugging only.")]
    [SerializeField] private string speaker = "Narrator";

    [Tooltip("Phrases of this line, played in order with their captions. Leave empty to skip the step.")]
    [SerializeField] private List<CaptionedClip> phrases = new List<CaptionedClip>();

    public string Speaker => speaker;
    public IReadOnlyList<CaptionedClip> Phrases => phrases;

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
        // Plays through the shared audio path; an empty list completes immediately.
        ctx.PlayVoice(data.Phrases, onComplete);
    }

    public void Exit()
    {
        ctx?.StopVoice();
    }
}
