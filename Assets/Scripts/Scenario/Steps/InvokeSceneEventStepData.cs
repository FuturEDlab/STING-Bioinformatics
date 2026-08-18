using System;
using System.Collections;
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

    [Space]
    [Tooltip("Seconds to hold before the scenario moves on. The beat has already been raised, so this is the pause between the world reacting - the EHR screen flipping, the ambience changing - and whoever speaks next. Without it the next line starts on the same frame as the screen change and the player never gets to see the thing being talked about. 0 continues immediately.")]
    [Min(0f)]
    [SerializeField] private float pauseBeforeNextStep = 0.8f;

    public UnityEvent OnInvoke => onInvoke;
    public GameEvent InvokeChannel => invokeChannel;
    public bool WaitForExternalCompletion => waitForExternalCompletion;
    public GameEvent CompletionChannel => completionChannel;
    public float PauseBeforeNextStep => pauseBeforeNextStep;

    public override IScenarioStep CreateRuntimeStep() => new InvokeSceneEventStep(this);
}

/// <summary>Runtime executor for <see cref="InvokeSceneEventStepData"/>.</summary>
public class InvokeSceneEventStep : IScenarioStep
{
    private readonly InvokeSceneEventStepData data;
    private ScenarioContext ctx;
    private Action onComplete;
    private bool subscribed;
    private Coroutine pauseRoutine;

    public InvokeSceneEventStep(InvokeSceneEventStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext ctx, Action onComplete)
    {
        this.ctx = ctx;
        this.onComplete = onComplete;

        data.OnInvoke?.Invoke();

        if (data.InvokeChannel != null)
        {
            // Count listeners BEFORE raising: a channel with none is the single most common
            // reason "nothing happened", and it is otherwise completely silent.
            int listeners = data.InvokeChannel.ListenerCount;

            if (listeners == 0 && data.WaitForExternalCompletion)
            {
                Debug.LogError($"[InvokeSceneEventStep] '{data.name}' raised '{data.InvokeChannel.name}' but NOTHING IS LISTENING, and this step waits for '{(data.CompletionChannel != null ? data.CompletionChannel.name : "<none>")}' before it will continue — so the scenario will stall here.\n" +
                               $"Add a SceneEventRelay on an ALWAYS-ACTIVE object with Channel = {data.InvokeChannel.name}. A relay placed on the object it is meant to switch on cannot hear anything, because it only subscribes while enabled.");
            }
            else if (listeners == 0)
            {
                Debug.Log($"[InvokeSceneEventStep] '{data.name}' raised '{data.InvokeChannel.name}' with no listeners (fine if that part of the scene is not built yet).");
            }

            data.InvokeChannel.Raise();
        }

        if (!data.WaitForExternalCompletion)
        {
            CompleteAfterPause();
            return;
        }

        if (data.CompletionChannel == null)
        {
            Debug.LogWarning("[InvokeSceneEventStep] waitForExternalCompletion is true but no completionChannel is set; completing immediately.");
            CompleteAfterPause();
            return;
        }

        data.CompletionChannel.Subscribe(OnExternalComplete);
        subscribed = true;
    }

    private void OnExternalComplete()
    {
        Unsubscribe();
        CompleteAfterPause();
    }

    /// <summary>
    /// Hold for Pause Before Next Step, then hand control back to the controller. The pause
    /// sits on THIS side of the handover rather than as a lead-in on whatever follows,
    /// because the next step is not always a line of dialogue - the step that changed the
    /// world is the one that should pay for letting the player see it.
    /// </summary>
    private void CompleteAfterPause()
    {
        float pause = data.PauseBeforeNextStep;

        // No runner means nothing can wait, so complete now rather than stall the scenario.
        if (pause <= 0f || ctx == null || ctx.Runner == null)
        {
            onComplete?.Invoke();
            return;
        }

        pauseRoutine = ctx.Runner.StartCoroutine(PauseThenComplete(pause));
    }

    private IEnumerator PauseThenComplete(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        pauseRoutine = null;
        onComplete?.Invoke();
    }

    public void Exit()
    {
        Unsubscribe();
        StopPause();
    }

    private void StopPause()
    {
        // Skipping the step mid-pause (debug HUD, restart) must not leave a coroutine alive
        // that later fires onComplete into a step the controller has already moved past.
        if (pauseRoutine != null && ctx != null && ctx.Runner != null)
            ctx.Runner.StopCoroutine(pauseRoutine);

        pauseRoutine = null;
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
