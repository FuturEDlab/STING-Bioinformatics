using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Connects the EHR terminal to the Scenario Controller, in both directions, without either
/// side referencing the other:
///
///  * SCENARIO -> SCREEN. Each row in Screens says "when the scenario raises this world beat
///    (EV_EHR_*), or starts waiting for this task id, show that EHR screen". That replaces
///    the terminal's own timers, which were only ever standing in for a line of dialogue.
///
///  * BUTTON -> SCENARIO. Each row in Actions says "when this EHR action is pressed, raise
///    that task id on EV_BioTask". That turns the terminal's buttons from a way of stepping
///    its own sequence into a way of satisfying a gate the scenario is actually waiting for.
///
/// Everything travels over the same two shared channel assets the rest of the scenario uses,
/// so the terminal still knows nothing about the ScenarioController and the step assets still
/// know nothing about the terminal.
/// </summary>
public class EHRScenarioBridge : MonoBehaviour
{
    [Serializable]
    public class ScreenCue
    {
        [Tooltip("Free text - only there to make the list readable.")]
        public string label;

        [Tooltip("A world beat channel, e.g. EV_EHR_MethoAlert. When the scenario raises it, the terminal switches to State.")]
        public GameEvent worldBeat;

        [Tooltip("A task id, e.g. wristband.scan. When the scenario starts waiting for it, the terminal switches to State. Use this for the screens that prompt the player to do something.")]
        public string whenWaitingFor;

        [Tooltip("Step Name of the screen to switch to. Must match a Step Name on the sequence player exactly.")]
        public string state;
    }

    [Serializable]
    public class ActionTask
    {
        [Tooltip("Action Name of an EHR step, e.g. Confirm override. Pressing a button wired to that action raises the task below.")]
        public string ehrAction;

        [Tooltip("The scenario task id this press satisfies, e.g. alert.override. Raised on the task channel; the scenario ignores it unless it is waiting for exactly this id.")]
        public string taskId;
    }

    [Header("The terminal")]
    [Tooltip("Left empty, the sequence player on this object is used.")]
    [SerializeField] private EHRSequencePlayer player;

    [Header("Scenario channels (the same assets the ScenarioController uses)")]
    [Tooltip("EV_BioTask - what the scenario's Wait For Task steps listen on. Needed for the button half.")]
    [SerializeField] private StringGameEvent taskChannel;

    [Tooltip("EV_Focus - broadcasts the task the scenario is waiting for right now. Needed for the prompt screens.")]
    [SerializeField] private StringGameEvent focusChannel;

    [Header("Screens")]
    [Tooltip("Screen shown when the scene loads. Leave empty to let the sequence player start itself.")]
    [SerializeField] private string startState;

    [Tooltip("Screen shown whenever the scenario stops waiting for a task, i.e. while a line of dialogue plays. A world beat arriving in the same moment wins, so a screen the scenario just put up is never wiped by this. Leave empty to always hold the last screen.")]
    [SerializeField] private string stateWhileNarrating;

    [Tooltip("One row per screen the scenario should be able to put on the terminal.")]
    [SerializeField] private List<ScreenCue> screens = new List<ScreenCue>();

    [Header("Buttons")]
    [Tooltip("One row per EHR action that completes something the scenario is waiting for.")]
    [SerializeField] private List<ActionTask> actions = new List<ActionTask>();

    [Header("Diagnostics")]
    [Tooltip("Print a report at startup listing every cue and whether it can actually resolve.")]
    [SerializeField] private bool reportWiringOnStart = true;

    [Tooltip("Log every screen change and every task raised.")]
    [SerializeField] private bool debugLogging;

    private bool subscribed;
    private bool cuedOnce;

    private void Reset()
    {
        player = GetComponent<EHRSequencePlayer>();
    }

    private void Awake()
    {
        if (player == null)
            player = GetComponent<EHRSequencePlayer>();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    /// <summary>Reset the terminal to its first authored sequence screen.</summary>
    public void RestartSequence()
    {
        if (player != null)
            player.StartSequence();
    }

    private void Start()
    {
        if (reportWiringOnStart)
            ReportWiring();

        // Deliberately in Start rather than OnEnable: the sequence player may still be
        // running its own autoStartOnEnable at that point and would show step 0 over the top.
        // Skipped if the scenario has already cued a screen, since whatever it asked for is
        // newer than the opening screen.
        if (!cuedOnce && !string.IsNullOrWhiteSpace(startState) && player != null)
            player.GoToState(startState);
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        for (int i = 0; i < screens.Count; i++)
        {
            ScreenCue cue = screens[i];
            if (cue != null && cue.worldBeat != null)
                cue.worldBeat.Subscribe(MakeBeatHandler(cue));
        }

        if (focusChannel != null)
            focusChannel.Subscribe(OnFocusChanged);

        if (player != null)
            player.ActionTriggered += OnActionTriggered;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        for (int i = 0; i < screens.Count; i++)
        {
            ScreenCue cue = screens[i];
            if (cue != null && cue.worldBeat != null)
                cue.worldBeat.Unsubscribe(MakeBeatHandler(cue));
        }

        if (focusChannel != null)
            focusChannel.Unsubscribe(OnFocusChanged);

        if (player != null)
            player.ActionTriggered -= OnActionTriggered;

        subscribed = false;
    }

    // GameEvent takes a parameterless Action, so each row needs its own closure. The
    // delegates are cached per row because an Action built fresh on unsubscribe would not
    // match the one that was subscribed, and the listener would leak.
    private Dictionary<ScreenCue, Action> beatHandlers;

    private Action MakeBeatHandler(ScreenCue cue)
    {
        beatHandlers ??= new Dictionary<ScreenCue, Action>();

        if (!beatHandlers.TryGetValue(cue, out Action handler))
        {
            handler = () => ShowScreen(cue.state, $"beat '{cue.worldBeat.name}'");
            beatHandlers.Add(cue, handler);
        }

        return handler;
    }

    private void OnFocusChanged(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            // The scenario is between gates, which means somebody is talking.
            if (!string.IsNullOrWhiteSpace(stateWhileNarrating))
                ShowScreen(stateWhileNarrating, "no task awaited");
            return;
        }

        for (int i = 0; i < screens.Count; i++)
        {
            ScreenCue cue = screens[i];
            if (cue == null || string.IsNullOrWhiteSpace(cue.whenWaitingFor))
                continue;

            if (string.Equals(cue.whenWaitingFor.Trim(), taskId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ShowScreen(cue.state, $"waiting for '{taskId}'");
                return;
            }
        }
    }

    private void OnActionTriggered(string ehrAction)
    {
        if (string.IsNullOrWhiteSpace(ehrAction))
            return;

        for (int i = 0; i < actions.Count; i++)
        {
            ActionTask row = actions[i];
            if (row == null || string.IsNullOrWhiteSpace(row.ehrAction))
                continue;

            if (!string.Equals(row.ehrAction.Trim(), ehrAction.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            if (taskChannel == null)
            {
                Debug.LogWarning($"[EHRScenarioBridge] '{ehrAction}' is mapped to task '{row.taskId}' but no Task Channel is assigned, so the scenario was not told. Assign EV_BioTask.", this);
                return;
            }

            if (debugLogging)
                Debug.Log($"[EHRScenarioBridge] '{ehrAction}' -> raising '{row.taskId}'.", this);

            // Raised whatever the scenario happens to be doing: a Wait For Task step ignores
            // an id it is not waiting for, so a press at the wrong moment costs nothing.
            taskChannel.Raise(row.taskId);
            return;
        }

        if (debugLogging)
            Debug.Log($"[EHRScenarioBridge] '{ehrAction}' has no task mapped to it; nothing raised.", this);
    }

    private void ShowScreen(string state, string because)
    {
        if (player == null || string.IsNullOrWhiteSpace(state))
            return;

        if (debugLogging)
            Debug.Log($"[EHRScenarioBridge] {because} -> screen '{state}'.", this);

        cuedOnce = true;
        player.GoToState(state);
    }

    /// <summary>
    /// Answers "why did the screen not change?" without a debugger: every cue is checked
    /// against the screens the terminal actually has, and every mapped button against the
    /// actions it actually listens for. A mistyped name is invisible at runtime otherwise.
    /// </summary>
    [ContextMenu("Report EHR Wiring")]
    public void ReportWiring()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== EHR <-> SCENARIO WIRING ===");

        int problems = 0;

        if (player == null)
        {
            Debug.LogWarning("[EHRScenarioBridge] No sequence player assigned or found on this object; nothing can be driven.", this);
            return;
        }

        if (!player.scenarioDriven)
        {
            report.AppendLine("PROBLEM  The sequence player has Scenario Driven switched OFF, so it still runs its own");
            report.AppendLine("         step timers and advances on its own button presses. The screen will drift out");
            report.AppendLine("         of the story. Tick Scenario Driven on the EHR Sequence Player.");
            problems++;
        }

        if (focusChannel == null)
        {
            report.AppendLine("PROBLEM  Focus Channel is empty - none of the prompt screens (the ones cued by a task");
            report.AppendLine("         id) will ever appear. Assign EV_Focus.");
            problems++;
        }

        if (taskChannel == null && actions.Count > 0)
        {
            report.AppendLine("PROBLEM  Task Channel is empty - pressing a button on the terminal will not tell the");
            report.AppendLine("         scenario anything. Assign EV_BioTask.");
            problems++;
        }

        var cued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < screens.Count; i++)
        {
            ScreenCue cue = screens[i];

            if (cue == null || string.IsNullOrWhiteSpace(cue.state))
            {
                report.AppendLine($"PROBLEM  Screens row {i + 1} has no screen name.");
                problems++;
                continue;
            }

            if (!player.HasState(cue.state))
            {
                report.AppendLine($"PROBLEM  Screens row {i + 1} points at '{cue.state}', which is not a Step Name on the terminal.");
                problems++;
                continue;
            }

            if (cue.worldBeat == null && string.IsNullOrWhiteSpace(cue.whenWaitingFor))
            {
                report.AppendLine($"PROBLEM  '{cue.state}' has neither a world beat nor a task id, so nothing can ever cue it.");
                problems++;
                continue;
            }

            cued.Add(cue.state.Trim());

            string trigger = cue.worldBeat != null ? cue.worldBeat.name : $"waiting for '{cue.whenWaitingFor}'";
            report.AppendLine($"  ok     {trigger} -> '{cue.state}'");
        }

        for (int i = 0; i < actions.Count; i++)
        {
            ActionTask row = actions[i];

            if (row == null || string.IsNullOrWhiteSpace(row.ehrAction) || string.IsNullOrWhiteSpace(row.taskId))
            {
                report.AppendLine($"PROBLEM  Actions row {i + 1} is half filled in.");
                problems++;
                continue;
            }

            if (!player.HasAction(row.ehrAction))
            {
                report.AppendLine($"  note   no screen advances on '{row.ehrAction}'. Fine if a prop raises it, but check the spelling.");
                continue;
            }

            report.AppendLine($"  ok     button '{row.ehrAction}' -> task '{row.taskId}'");
        }

        // A screen nothing cues can never appear while the scenario is driving. Usually it is
        // a renamed step whose row was not updated with it.
        if (player.steps != null)
        {
            for (int i = 0; i < player.steps.Count; i++)
            {
                EHRSequencePlayer.SequenceStep step = player.steps[i];
                if (step == null || string.IsNullOrWhiteSpace(step.stepName))
                    continue;

                string stepName = step.stepName.Trim();

                if (cued.Contains(stepName))
                    continue;

                if (string.Equals(stepName, startState?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(stepName, stateWhileNarrating?.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                report.AppendLine($"  note   screen '{step.stepName}' has no cue, so the scenario can never show it.");
            }
        }

        report.AppendLine(problems == 0 ? "No problems found." : $"{problems} problem(s) found.");

        if (problems == 0)
            Debug.Log(report.ToString(), this);
        else
            Debug.LogWarning(report.ToString(), this);
    }
}
