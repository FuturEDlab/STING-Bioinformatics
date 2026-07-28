using System;
using UnityEngine;

/// <summary>
/// Command step: raises a channel and lets the scene do the work. A SceneEventRelay
/// listening on that channel runs the actual animation / prop change.
///
/// Either fires and forgets, or waits for the scene to report back on a completion channel.
/// </summary>
[CreateAssetMenu(fileName = "InvokeSceneEventStep", menuName = "Scenario/Steps/Invoke Scene Event")]
public class InvokeSceneEventStepData : ScenarioStepData
{
    [Tooltip("Raised on Enter. A SceneEventRelay in the scene listens on this asset.")]
    public GameEvent invokeChannel;

    [Header("Completion")]
    [Tooltip("Off: the step completes as soon as the channel is raised. " +
             "On: it waits for the completion channel below.")]
    public bool waitForExternalCompletion;

    [Tooltip("Raised by SceneEventRelay.ReportComplete() when the scene action finishes.")]
    public GameEvent completionChannel;

    public override IScenarioStep CreateRuntimeStep() => new InvokeSceneEventStep(this);
}

public class InvokeSceneEventStep : IScenarioStep
{
    private readonly InvokeSceneEventStepData data;
    private Action onComplete;
    private bool subscribed;

    public InvokeSceneEventStep(InvokeSceneEventStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext context, Action onComplete)
    {
        this.onComplete = onComplete;

        bool waiting = data.waitForExternalCompletion && data.completionChannel != null;

        if (data.waitForExternalCompletion && data.completionChannel == null)
            Debug.LogError($"[Scenario] '{data.name}' waits for completion but has no completion channel - " +
                           "completing immediately instead of hanging.");

        // Subscribe before raising: a relay that reports completion synchronously
        // would otherwise finish before anyone is listening.
        if (waiting)
        {
            data.completionChannel.Subscribe(OnExternalComplete);
            subscribed = true;
        }

        if (data.invokeChannel != null)
            data.invokeChannel.Raise();
        else
            Debug.LogWarning($"[Scenario] '{data.name}' has no invoke channel assigned.");

        if (!subscribed)
            Complete();
    }

    private void OnExternalComplete() => Complete();

    public void Exit()
    {
        Unsubscribe();
    }

    private void Complete()
    {
        Unsubscribe();

        Action callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        subscribed = false;
        data.completionChannel.Unsubscribe(OnExternalComplete);
    }
}
