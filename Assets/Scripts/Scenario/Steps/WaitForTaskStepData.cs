using System;
using UnityEngine;

/// <summary>
/// Blocks the scenario until the player does something - grabs the scanner, opens the notes,
/// presses a key. The interactable raises a StringGameEvent; this step listens for its id.
/// </summary>
[CreateAssetMenu(fileName = "WaitForTaskStep", menuName = "Scenario/Steps/Wait For Task")]
public class WaitForTaskStepData : ScenarioStepData
{
    [Tooltip("Channel the interactable raises on.")]
    public StringGameEvent channel;

    [Tooltip("Id to wait for, e.g. \"scanner\". Leave empty to accept any id on the channel.")]
    public string taskId;

    [Tooltip("Optional hint VO played once when the step begins.")]
    public AudioClip promptVo;

    public override IScenarioStep CreateRuntimeStep() => new WaitForTaskStep(this);
}

public class WaitForTaskStep : IScenarioStep
{
    private readonly WaitForTaskStepData data;
    private ScenarioContext ctx;
    private Action onComplete;
    private bool subscribed;

    public WaitForTaskStep(WaitForTaskStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext context, Action onComplete)
    {
        ctx = context;
        this.onComplete = onComplete;

        if (data.channel == null)
        {
            Debug.LogError($"[Scenario] '{data.name}' has no channel assigned - completing immediately.");
            Complete();
            return;
        }

        data.channel.Subscribe(OnTaskRaised);
        subscribed = true;

        // Prompt is fire-and-forget: the step waits on the player, not on the audio.
        if (data.promptVo != null)
            ctx.PlayVoice(data.promptVo, null);
    }

    private void OnTaskRaised(string id)
    {
        if (!string.IsNullOrEmpty(data.taskId) && id != data.taskId)
            return;

        Complete();
    }

    public void Exit()
    {
        Unsubscribe();
        ctx = null;
    }

    private void Complete()
    {
        // Unsubscribe in the handler, not only in Exit - the channel must not reach a
        // step that has already decided it is done.
        Unsubscribe();

        Action callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        subscribed = false;
        data.channel.Unsubscribe(OnTaskRaised);
    }
}
