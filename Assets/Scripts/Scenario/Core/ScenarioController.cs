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

        // Re-read the Inspector bindings, then make sure no panel left visible in the
        // scene sits on top of the question we are about to ask.
        context.InvalidateLookups();
        context.HideAllQuestionPanels();

        index = 0;
        EnterCurrent();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Adds an empty binding row for every question and teleport step in the scenario that
    /// doesn't have one yet. Run it after adding steps, then drag the scene panel /
    /// destination into each new row.
    /// </summary>
    [ContextMenu("Fill Binding Rows From Scenario")]
    public void FillBindingRowsFromScenario()
    {
        if (scenario == null || scenario.Steps == null || scenario.Steps.Count == 0)
        {
            Debug.LogWarning("[ScenarioController] No scenario/steps assigned.", this);
            return;
        }

        UnityEditor.Undo.RecordObject(this, "Fill Scenario Binding Rows");
        int added = 0;

        for (int i = 0; i < scenario.Steps.Count; i++)
        {
            ScenarioStepData step = scenario.Steps[i];

            if (step is UIQuestionStepData question)
            {
                if (context.EnsureQuestionRow(question.Question))
                    added++;
            }
            else if (step is TeleportStepData teleport)
            {
                if (context.EnsureTeleportRow(teleport))
                    added++;
            }
        }

        if (added > 0)
            UnityEditor.EditorUtility.SetDirty(this);

        Debug.Log($"[ScenarioController] Added {added} empty binding row(s). Drag the scene panel / destination into each, then run Validate Scene Bindings.", this);
    }
#endif

    /// <summary>
    /// Walks the scenario and reports every step whose scene reference is missing —
    /// a question with no panel, a teleport with no destination. Run it from the
    /// component's context menu after wiring the lists, so gaps surface here instead of
    /// mid-playtest.
    /// </summary>
    [ContextMenu("Validate Scene Bindings")]
    public void ValidateSceneBindings()
    {
        if (scenario == null || scenario.Steps == null || scenario.Steps.Count == 0)
        {
            Debug.LogWarning("[ScenarioController] No scenario/steps assigned.", this);
            return;
        }

        context.InvalidateLookups();
        int problems = 0;

        for (int i = 0; i < scenario.Steps.Count; i++)
        {
            ScenarioStepData step = scenario.Steps[i];

            if (step == null)
            {
                Debug.LogWarning($"[ScenarioController] Step {i} is an empty slot.", this);
                problems++;
                continue;
            }

            if (step is UIQuestionStepData question)
            {
                if (question.Question == null)
                {
                    Debug.LogWarning($"[ScenarioController] Step {i} '{step.name}' has no question asset.", this);
                    problems++;
                }
                else if (context.GetQuestionPanel(question.Question) == null && context.Quiz == null)
                {
                    Debug.LogWarning($"[ScenarioController] Step {i} '{step.name}': question '{question.Question.name}' has no panel bound and there is no fallback Quiz.", this);
                    problems++;
                }
            }
            else if (step is TeleportStepData teleport)
            {
                if (context.GetTeleportDestination(teleport) == null)
                {
                    Debug.LogWarning($"[ScenarioController] Step {i} '{step.name}' has no teleport destination bound.", this);
                    problems++;
                }
            }
        }

        if (problems == 0)
            Debug.Log($"[ScenarioController] All {scenario.Steps.Count} steps resolved their scene bindings.", this);
        else
            Debug.LogWarning($"[ScenarioController] {problems} binding problem(s) found.", this);
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
