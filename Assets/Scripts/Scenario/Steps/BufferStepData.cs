using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Holds the scenario for a configured number of seconds before continuing.
/// Use this to add breathing room between voice-over lines or scene beats.
/// </summary>
[CreateAssetMenu(fileName = "BufferStep", menuName = "Scenario/Steps/Buffer")]
public class BufferStepData : ScenarioStepData
{
    [Tooltip("How long the scenario should wait before moving to the next step.")]
    [Min(0f)]
    [SerializeField] private float durationSeconds = 1f;

    public float DurationSeconds => durationSeconds;

    public override IScenarioStep CreateRuntimeStep() => new BufferStep(this);
}

/// <summary>Runtime executor for <see cref="BufferStepData"/>.</summary>
public class BufferStep : IScenarioStep
{
    private readonly BufferStepData data;
    private ScenarioContext context;
    private Action onComplete;
    private Coroutine bufferCoroutine;

    public BufferStep(BufferStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext context, Action onComplete)
    {
        this.context = context;
        this.onComplete = onComplete;

        if (data.DurationSeconds <= 0f || context == null || context.Runner == null)
        {
            onComplete?.Invoke();
            return;
        }

        bufferCoroutine = context.Runner.StartCoroutine(WaitThenComplete());
    }

    private IEnumerator WaitThenComplete()
    {
        yield return new WaitForSeconds(data.DurationSeconds);

        bufferCoroutine = null;
        onComplete?.Invoke();
    }

    public void Exit()
    {
        if (bufferCoroutine != null && context != null && context.Runner != null)
        {
            context.Runner.StopCoroutine(bufferCoroutine);
        }

        bufferCoroutine = null;
        onComplete = null;
    }
}
