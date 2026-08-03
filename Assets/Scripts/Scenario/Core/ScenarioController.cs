using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drives a ScenarioData asset as a linear sequence. Knows only about IScenarioStep and
/// the shared ScenarioContext — nothing about audio, UI, or gameplay specifics. Enters
/// the current step with an onComplete callback; on that callback it Exits the current
/// step, advances the index, and enters the next. Fires onScenarioComplete at the end.
/// </summary>
public class ScenarioController : MonoBehaviour
{
    [SerializeField] private ScenarioData scenario;
    [SerializeField] private ScenarioContext context;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private UnityEvent onScenarioComplete;

    private int index;
    private IScenarioStep current;
    private bool stepCompletedLatch;

    private void Start()
    {
        context.Runner = this;
        if (playOnStart)
            Begin();
    }

    /// <summary>(Re)start the scenario from the first step.</summary>
    [ContextMenu("Begin")]
    public void Begin()
    {
        if (scenario == null || scenario.Steps == null || scenario.Steps.Count == 0)
        {
            Debug.LogWarning("[ScenarioController] No scenario/steps assigned.", this);
            return;
        }

        // Tear down any step already running (e.g. restart mid-scenario) so its event
        // subscriptions are released before we start over.
        current?.Exit();
        current = null;

        index = 0;
        EnterCurrent();
    }

    private void EnterCurrent()
    {
        stepCompletedLatch = false;
        current = scenario.Steps[index].CreateRuntimeStep();
        current.Enter(context, OnStepComplete);
    }

    private void OnStepComplete()
    {
        // Guarantee a single completion per step entry.
        if (stepCompletedLatch)
            return;
        stepCompletedLatch = true;

        current.Exit();
        index++;

        if (index < scenario.Steps.Count)
            EnterCurrent();
        else
            onScenarioComplete?.Invoke();
    }
}
