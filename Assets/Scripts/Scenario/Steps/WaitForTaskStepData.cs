using System;
using UnityEngine;

/// <summary>
/// Data for a step that completes when a gameplay task raises the typed channel
/// (optionally matching a specific id).
/// </summary>
[CreateAssetMenu(fileName = "WaitForTaskStep", menuName = "Scenario/Steps/Wait For Task")]
public class WaitForTaskStepData : ScenarioStepData
{
    [SerializeField] private StringGameEvent taskChannel;

    [Tooltip("If empty, any raise on the channel completes the step; otherwise the raised id must match.")]
    [SerializeField] private string requiredTaskId;

    public StringGameEvent TaskChannel => taskChannel;
    public string RequiredTaskId => requiredTaskId;

    public override IScenarioStep CreateRuntimeStep() => new WaitForTaskStep(this);
}

/// <summary>
/// Runtime executor for <see cref="WaitForTaskStepData"/>. Pure observer: the gameplay
/// side calls Raise(id) with no knowledge of the scenario.
/// </summary>
public class WaitForTaskStep : IScenarioStep
{
    private readonly WaitForTaskStepData data;
    private Action onComplete;
    private bool subscribed;

    public WaitForTaskStep(WaitForTaskStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext ctx, Action onComplete)
    {
        this.onComplete = onComplete;

        if (data.TaskChannel == null)
        {
            Debug.LogWarning("[WaitForTaskStep] No task channel assigned; completing immediately.");
            onComplete?.Invoke();
            return;
        }

        data.TaskChannel.Subscribe(OnTaskRaised);
        subscribed = true;
    }

    private void OnTaskRaised(string id)
    {
        if (!string.IsNullOrEmpty(data.RequiredTaskId) && id != data.RequiredTaskId)
            return; // not our task; keep waiting

        Unsubscribe();
        onComplete?.Invoke();
    }

    public void Exit()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (subscribed)
        {
            data.TaskChannel.Unsubscribe(OnTaskRaised);
            subscribed = false;
        }
    }
}
