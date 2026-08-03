using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Data for a step that invokes a scene/asset event, then either completes immediately
/// or waits for an external completion signal.
/// </summary>
[CreateAssetMenu(fileName = "InvokeSceneEventStep", menuName = "Scenario/Steps/Invoke Scene Event")]
public class InvokeSceneEventStepData : ScenarioStepData
{
    // NOTE: A UnityEvent serialized on a ScriptableObject asset can only target other
    // ASSETS (e.g. a manager that is itself an asset), NEVER scene GameObjects. To invoke
    // a SCENE object's method, leave 'onInvoke' empty, assign 'invokeChannel', and add a
    // SceneEventRelay in the scene wired to that same channel (its response UnityEvent can
    // target scene methods).
    [SerializeField] private UnityEvent onInvoke;
    [SerializeField] private GameEvent invokeChannel;

    [Space]
    [Tooltip("If false, complete immediately after invoking. If true, wait until completionChannel is raised.")]
    [SerializeField] private bool waitForExternalCompletion;
    [SerializeField] private GameEvent completionChannel; // required only when waitForExternalCompletion

    public UnityEvent OnInvoke => onInvoke;
    public GameEvent InvokeChannel => invokeChannel;
    public bool WaitForExternalCompletion => waitForExternalCompletion;
    public GameEvent CompletionChannel => completionChannel;

    public override IScenarioStep CreateRuntimeStep() => new InvokeSceneEventStep(this);
}

/// <summary>Runtime executor for <see cref="InvokeSceneEventStepData"/>.</summary>
public class InvokeSceneEventStep : IScenarioStep
{
    private readonly InvokeSceneEventStepData data;
    private Action onComplete;
    private bool subscribed;

    public InvokeSceneEventStep(InvokeSceneEventStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext ctx, Action onComplete)
    {
        this.onComplete = onComplete;

        data.OnInvoke?.Invoke();
        if (data.InvokeChannel != null)
            data.InvokeChannel.Raise();

        if (!data.WaitForExternalCompletion)
        {
            onComplete?.Invoke();
            return;
        }

        if (data.CompletionChannel == null)
        {
            Debug.LogWarning("[InvokeSceneEventStep] waitForExternalCompletion is true but no completionChannel is set; completing immediately.");
            onComplete?.Invoke();
            return;
        }

        data.CompletionChannel.Subscribe(OnExternalComplete);
        subscribed = true;
    }

    private void OnExternalComplete()
    {
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
            data.CompletionChannel.Unsubscribe(OnExternalComplete);
            subscribed = false;
        }
    }
}
