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

    [Tooltip("Print a wiring report to the console when the scenario starts. It lists every gate the scenario has and whether a prop in the scene can actually satisfy it.")]
    [SerializeField] private bool reportWiringOnStart = true;

    [SerializeField] private UnityEvent onScenarioComplete;

    private int index;
    private IScenarioStep current;
    private bool stepCompletedLatch;
    private bool isPaused;
    private bool stepCompletionPending;

    /// <summary>Raised whenever a step is entered, with its index and data. Used by the debug HUD.</summary>
    public event System.Action<int, ScenarioStepData> StepEntered;

    public int CurrentIndex => index;
    public int StepCount => scenario != null && scenario.Steps != null ? scenario.Steps.Count : 0;
    public bool IsRunning => current != null;
    public bool IsPaused => isPaused;

    public ScenarioStepData CurrentStep =>
        (scenario != null && scenario.Steps != null && index >= 0 && index < scenario.Steps.Count)
            ? scenario.Steps[index]
            : null;

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
        isPaused = false;
        stepCompletionPending = false;

        // Re-read the Inspector bindings, then make sure no panel left visible in the
        // scene sits on top of the question we are about to ask.
        context.InvalidateLookups();
        context.HideAllQuestionPanels();

        if (reportWiringOnStart)
            ReportWiring();

        index = 0;
        EnterCurrent();
    }

    /// <summary>Pause progression without restarting or exiting the current step.</summary>
    public void Pause()
    {
        if (!IsRunning)
            return;

        isPaused = true;
    }

    /// <summary>Resume progression from the current step.</summary>
    public void Resume()
    {
        if (!isPaused)
            return;

        isPaused = false;

        if (stepCompletionPending)
        {
            stepCompletionPending = false;
            OnStepComplete();
        }
    }

    /// <summary>
    /// Answers "why is nothing glowing / why won't this scan?" without a debugger. Walks
    /// every gate in the scenario and checks that some prop in the scene is actually
    /// listening on the same channels with the same task id — a mismatched or empty
    /// channel slot is invisible at runtime but shows up plainly here.
    /// </summary>
    [ContextMenu("Report Scenario Wiring")]
    public void ReportWiring()
    {
        if (scenario == null || scenario.Steps == null)
        {
            Debug.LogWarning("[ScenarioController] No scenario assigned.", this);
            return;
        }

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== SCENARIO WIRING REPORT ===");

        int problems = 0;

        // --- the shared plumbing --------------------------------------------------------
        if (context.VoSource == null)
        {
            report.AppendLine("PROBLEM  Context ▸ Vo Source is empty — no voice-over will be audible.");
            problems++;
        }
        if (context.CaptionDisplay == null)
        {
            report.AppendLine("PROBLEM  Context ▸ Caption Display is empty — no captions will show.");
            problems++;
        }
        if (context.FocusChannel == null)
        {
            report.AppendLine("PROBLEM  Context ▸ Focus Channel is empty — NOTHING will ever glow, and every");
            report.AppendLine("         ScenarioTarget with 'Require Focus' on will refuse input. Assign EV_Focus.");
            problems++;
        }

        // A Panel Question step with no panel assigned is a silent no-show, so call it out.
        bool needsPanel = false;
        for (int i = 0; i < scenario.Steps.Count; i++)
        {
            if (scenario.Steps[i] is PanelQuestionStepData) { needsPanel = true; break; }
        }

        if (needsPanel && context.QuestionPanel == null)
        {
            report.AppendLine("PROBLEM  The scenario asks a question through the Question Panels prefab, but");
            report.AppendLine("         Context ▸ Question Panel is empty — the panel will never appear.");
            report.AppendLine("         Drag the QuestionPanelManager from the prefab into that field.");
            problems++;
        }

        // --- what the scenario asks for, vs what the scene can answer -------------------
        ScenarioTarget[] targets = FindObjectsByType<ScenarioTarget>(FindObjectsSortMode.None);
        report.AppendLine($"Found {targets.Length} ScenarioTarget(s) in the scene.");

        for (int i = 0; i < scenario.Steps.Count; i++)
        {
            if (scenario.Steps[i] is not WaitForTaskStepData wait)
                continue;

            string id = wait.RequiredTaskId;
            ScenarioTarget match = null;

            for (int t = 0; t < targets.Length; t++)
            {
                if (targets[t].TaskId == id) { match = targets[t]; break; }
            }

            if (match == null)
            {
                report.AppendLine($"PROBLEM  step {i + 1} waits for '{id}' but no ScenarioTarget in the scene has that Task Id.");
                report.AppendLine("         (Fine for now — the debug HUD's Space key can satisfy it.)");
                problems++;
                continue;
            }

            if (match.FocusChannel != context.FocusChannel)
            {
                report.AppendLine($"PROBLEM  '{match.name}' ({id}) uses a DIFFERENT Focus Channel than the controller — it will never glow or accept input.");
                problems++;
            }

            if (match.TaskChannel != wait.TaskChannel)
            {
                report.AppendLine($"PROBLEM  '{match.name}' ({id}) raises a different Task Channel than the step listens on — completing it will do nothing.");
                problems++;
            }

            if (match.FocusChannel == context.FocusChannel && match.TaskChannel == wait.TaskChannel)
                report.AppendLine($"  ok     '{id}' → {match.name}");
        }

        // --- world beats: is anything in the scene listening? ---------------------------
        SceneEventRelay[] relays = FindObjectsByType<SceneEventRelay>(FindObjectsSortMode.None);

        for (int i = 0; i < scenario.Steps.Count; i++)
        {
            if (scenario.Steps[i] is not InvokeSceneEventStepData ev)
                continue;

            if (ev.InvokeChannel != null && !AnythingListensTo(relays, ev.InvokeChannel))
            {
                // Not a problem on its own — that part of the room may simply not be built
                // yet — but a step that blocks until something answers it definitely is.
                if (ev.WaitForExternalCompletion)
                {
                    report.AppendLine($"PROBLEM  step {i + 1} raises '{ev.InvokeChannel.name}' and then WAITS for");
                    report.AppendLine($"         '{(ev.CompletionChannel != null ? ev.CompletionChannel.name : "<none>")}'. Nothing listens to");
                    report.AppendLine("         the first channel, so nothing will ever open — the scenario will stall here.");
                    problems++;
                }
                else
                {
                    report.AppendLine($"  note   '{ev.InvokeChannel.name}' has nothing listening (no relay, and the EHR has no cue for it).");
                }
            }
        }

        report.AppendLine(problems == 0
            ? "No problems found."
            : $"{problems} problem(s) found.");

        if (problems == 0)
            Debug.Log(report.ToString(), this);
        else
            Debug.LogWarning(report.ToString(), this);
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

    /// <summary>
    /// Force the current step to finish and move on. Exists so a step whose gameplay is
    /// not built yet (or that the player is stuck on) never blocks a playtest — the debug
    /// HUD calls this. Does nothing once the scenario has ended.
    /// </summary>
    [ContextMenu("Skip Current Step")]
    public void SkipCurrentStep()
    {
        if (current == null)
            return;

        OnStepComplete();
    }

    /// <summary>
    /// Jump straight to a step. For testing only — steps before the target never run, so
    /// anything they would have set up in the scene will not have happened.
    /// </summary>
    public void JumpToStep(int target)
    {
        if (scenario == null || scenario.Steps == null || target < 0 || target >= scenario.Steps.Count)
            return;

        current?.Exit();
        current = null;
        index = target;
        EnterCurrent();
    }

    /// <summary>
    /// Fast-forward through dialogue to the next point where the player has to do
    /// something. Saves sitting through a minute of narration every time you test a prop.
    /// </summary>
    [ContextMenu("Skip To Next Gate")]
    public void SkipToNextGate()
    {
        if (scenario == null || scenario.Steps == null)
            return;

        // Bounded so a scenario with no gates left can't spin forever.
        for (int guard = 0; guard < scenario.Steps.Count + 1; guard++)
        {
            if (current == null)
                return;

            SkipCurrentStep();

            // PanelQuestionStepData belongs here as much as the other two: it is the
            // in-simulation quiz, and leaving it out meant skip-to-gate ran straight past
            // the question instead of stopping at it.
            if (CurrentStep is WaitForTaskStepData ||
                CurrentStep is UIQuestionStepData ||
                CurrentStep is PanelQuestionStepData)
                return;
        }
    }

    /// <summary>
    /// Is anything at all going to hear this world beat? A SceneEventRelay is the usual
    /// answer, but not the only one — the EHR terminal subscribes its own screen cues
    /// directly — so ask the channel itself first and fall back to naming relays. The
    /// listener count is only meaningful in play mode, hence both checks.
    /// </summary>
    private static bool AnythingListensTo(SceneEventRelay[] relays, GameEvent channel)
    {
        if (channel.ListenerCount > 0)
            return true;

        for (int i = 0; i < relays.Length; i++)
        {
            if (relays[i] != null && relays[i].Channel == channel)
                return true;
        }
        return false;
    }

    private void EnterCurrent()
    {
        stepCompletedLatch = false;
        ScenarioStepData data = scenario.Steps[index];
        current = data.CreateRuntimeStep();
        StepEntered?.Invoke(index, data);
        current.Enter(context, OnStepComplete);
    }

    private void OnStepComplete()
    {
        // Guarantee a single completion per step entry.
        if (stepCompletedLatch)
            return;

        if (isPaused)
        {
            stepCompletionPending = true;
            return;
        }

        stepCompletedLatch = true;

        current.Exit();
        index++;

        if (index < scenario.Steps.Count)
        {
            EnterCurrent();
        }
        else
        {
            // Drop the finished step so IsRunning reports honestly once the scenario ends.
            current = null;
            onScenarioComplete?.Invoke();
        }
    }
}
