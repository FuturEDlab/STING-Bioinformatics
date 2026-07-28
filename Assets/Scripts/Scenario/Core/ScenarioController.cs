using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Linear sequencer. Walks a ScenarioData list one step at a time:
/// build runtime step -> Enter(context, onComplete) -> wait -> Exit -> advance.
///
/// Deliberately knows nothing about audio, UI or gameplay types - it runs against
/// IScenarioStep only. Add new behaviour by writing a new step, never by editing this file.
/// </summary>
public class ScenarioController : MonoBehaviour
{
    [Header("Scenario")]
    [Tooltip("The ordered step list to run.")]
    [SerializeField] private ScenarioData scenario;

    [Tooltip("Start automatically on Start(). Turn off when a menu button calls Begin().")]
    [SerializeField] private bool beginOnStart = false;

    [Header("Shared services handed to every step")]
    [SerializeField] private ScenarioContext context = new ScenarioContext();

    [Header("Completion")]
    [Tooltip("Fires once when the last step finishes.")]
    public UnityEvent onScenarioComplete;

    private IScenarioStep currentStep;
    private int index = -1;
    private bool running;

    public bool IsRunning => running;
    public int CurrentIndex => index;
    public ScenarioContext Context => context;

    private void Awake()
    {
        // Dependency injection: steps receive the runner, they never look it up.
        context.Runner = this;
    }

    private void Start()
    {
        if (beginOnStart)
            Begin();
    }

    /// <summary>
    /// Starts the scenario from step 0. Safe to call again to restart - any running step
    /// is exited first so its subscriptions cannot leak.
    /// </summary>
    public void Begin()
    {
        if (scenario == null)
        {
            Debug.LogError("[Scenario] No ScenarioData assigned.", this);
            return;
        }

        ExitCurrentStep();

        running = true;
        index = 0;
        EnterCurrentStep();
    }

    /// <summary>Aborts the scenario. onScenarioComplete does NOT fire.</summary>
    public void StopScenario()
    {
        ExitCurrentStep();
        context.StopVoice();

        running = false;
        index = -1;
    }

    /// <summary>Alias for Begin(), for menu buttons that read as "restart".</summary>
    public void Restart() => Begin();

    private void EnterCurrentStep()
    {
        if (!running)
            return;

        if (index >= scenario.StepCount)
        {
            Finish();
            return;
        }

        ScenarioStepData data = scenario.GetStep(index);
        if (data == null)
        {
            Debug.LogWarning($"[Scenario] Step {index} is empty - skipping.", this);
            index++;
            EnterCurrentStep();
            return;
        }

        // Factory Method: the data asset decides which runtime class to build.
        IScenarioStep step = data.CreateRuntimeStep();
        currentStep = step;

        // Per-step completion latch, captured by the callback. Guards two failure modes:
        // a step that invokes onComplete twice, and a stale callback arriving after we
        // already exited that step.
        bool stepCompletedLatch = false;

        step.Enter(context, () =>
        {
            if (stepCompletedLatch) return;
            stepCompletedLatch = true;

            if (!ReferenceEquals(currentStep, step)) return;

            Advance();
        });
    }

    private void Advance()
    {
        ExitCurrentStep();
        index++;

        // A step that completes synchronously inside Enter() recurses through here.
        // Bounded by step count and harmless at realistic scenario sizes - see design report S9.
        EnterCurrentStep();
    }

    private void ExitCurrentStep()
    {
        if (currentStep == null)
            return;

        // Null the field before calling Exit so a re-entrant Exit cannot loop.
        IScenarioStep step = currentStep;
        currentStep = null;
        step.Exit();
    }

    private void Finish()
    {
        running = false;
        currentStep = null;

        Debug.Log("[Scenario] Complete.", this);
        onScenarioComplete?.Invoke();
    }

    private void OnDisable()
    {
        // Never leave a step subscribed to a channel when this object goes away.
        ExitCurrentStep();
        running = false;
    }
}
