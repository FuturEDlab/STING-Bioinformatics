using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An on-screen driver for playtesting the scenario before every prop is wired.
///
/// It shows which step is running and what the scenario is waiting for, and lets you
/// satisfy that wait from the keyboard — so a gate whose gameplay does not exist yet (the
/// EHR keypad, say) never blocks a run-through. It also gives you a button per world beat,
/// which is how you stand in for the EHR while its screens are still being built.
///
/// Editor/desktop only by intent: leave it on the ScenarioController's GameObject during
/// development and disable it for headset builds.
/// </summary>
public class ScenarioDebugHUD : MonoBehaviour
{
    [System.Serializable]
    public class EventButton
    {
        [Tooltip("Label shown on the button.")]
        public string label;

        [Tooltip("The GameEvent asset to raise — e.g. EV_EHR_PatientVerified.")]
        public GameEvent channel;
    }

    [SerializeField] private ScenarioController controller;

    [Tooltip("EV_BioTask — used to satisfy the gate the scenario is waiting on.")]
    [SerializeField] private StringGameEvent taskChannel;

    [Tooltip("EV_Focus — tells the HUD which task is currently being awaited.")]
    [SerializeField] private StringGameEvent focusChannel;

    [Header("Keys")]
    [Tooltip("Completes whatever the scenario is waiting for right now.")]
    [SerializeField] private KeyCode completeTaskKey = KeyCode.Space;

    [Tooltip("Force the current step to end, even if it is a line of dialogue.")]
    [SerializeField] private KeyCode skipStepKey = KeyCode.RightArrow;

    [Tooltip("Fast-forward past the dialogue to the next thing the player has to do.")]
    [SerializeField] private KeyCode skipToGateKey = KeyCode.Tab;

    [Tooltip("Show/hide the panel.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Header("World beats")]
    [Tooltip("One button per scene event you want to fire by hand. Fill these with the EV_EHR_* assets while the EHR is still being built.")]
    [SerializeField] private List<EventButton> eventButtons = new List<EventButton>();

    [Header("Display")]
    [SerializeField] private bool visible = true;
    [SerializeField] private int fontSize = 14;

    private string awaitedTask = "";
    private string currentStepName = "(not started)";
    private int currentIndex = -1;
    private Vector2 scroll;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<ScenarioController>();
    }

    private void OnEnable()
    {
        if (controller != null)
            controller.StepEntered += OnStepEntered;

        if (focusChannel != null)
            focusChannel.Subscribe(OnFocusChanged);
    }

    private void OnDisable()
    {
        if (controller != null)
            controller.StepEntered -= OnStepEntered;

        if (focusChannel != null)
            focusChannel.Unsubscribe(OnFocusChanged);
    }

    private void OnStepEntered(int index, ScenarioStepData data)
    {
        currentIndex = index;
        currentStepName = data != null ? data.name : "(empty step)";

        // A step that blocks on a scene event is not a "task", so it never reaches the
        // focus channel. Surface it here too, otherwise the panel reads "waiting on: —"
        // while the scenario is in fact stuck solid.
        pendingCompletion = null;
        if (data is InvokeSceneEventStepData ev && ev.WaitForExternalCompletion)
            pendingCompletion = ev.CompletionChannel;
    }

    private GameEvent pendingCompletion;

    private void OnFocusChanged(string taskId) => awaitedTask = taskId ?? "";

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            visible = !visible;

        if (Input.GetKeyDown(completeTaskKey))
            CompleteAwaitedTask();

        if (Input.GetKeyDown(skipStepKey) && controller != null)
            controller.SkipCurrentStep();

        if (Input.GetKeyDown(skipToGateKey) && controller != null)
            controller.SkipToNextGate();
    }

    /// <summary>
    /// Raise whatever task the scenario is blocked on. When nothing is being awaited the
    /// step is a line of dialogue or a world beat, so skip it instead — that way one key
    /// always moves the scenario forward.
    /// </summary>
    public void CompleteAwaitedTask()
    {
        if (!string.IsNullOrEmpty(awaitedTask) && taskChannel != null)
        {
            taskChannel.Raise(awaitedTask);
            return;
        }

        // Satisfy a scene-event wait properly rather than skipping past it, so the step
        // ends the way it would in a finished build.
        if (pendingCompletion != null)
        {
            pendingCompletion.Raise();
            return;
        }

        if (controller != null)
            controller.SkipCurrentStep();
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true };
        GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = fontSize };

        int total = controller != null ? controller.StepCount : 0;

        GUILayout.BeginArea(new Rect(10, 10, 380, Screen.height - 20), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label($"<b>SCENARIO</b>  step {currentIndex + 1} / {total}", Bold(style));
        GUILayout.Label(currentStepName, style);

        GUILayout.Space(6);
        if (!string.IsNullOrEmpty(awaitedTask))
        {
            GUILayout.Label($"<b>Waiting on task:</b> {awaitedTask}", Bold(style));
        }
        else if (pendingCompletion != null)
        {
            GUILayout.Label($"<b>Waiting on scene event:</b> {pendingCompletion.name}", Bold(style));
            GUILayout.Label(pendingCompletion.ListenerCount == 0
                ? "  ⚠ nothing in the scene is listening to the channel that opens this — add a SceneEventRelay on an always-active object."
                : $"  {pendingCompletion.ListenerCount} listener(s) on the completion channel.", style);
        }
        else
        {
            GUILayout.Label("Waiting on: — (dialogue or world beat)", style);
        }

        GUILayout.Space(6);
        if (GUILayout.Button($"Complete / advance  [{completeTaskKey}]", button))
            CompleteAwaitedTask();

        if (GUILayout.Button($"Force skip step  [{skipStepKey}]", button) && controller != null)
            controller.SkipCurrentStep();

        if (GUILayout.Button($"Skip to next gate  [{skipToGateKey}]", button) && controller != null)
            controller.SkipToNextGate();

        if (GUILayout.Button("Restart scenario", button) && controller != null)
            controller.Begin();

        if (GUILayout.Button("Print wiring report", button) && controller != null)
            controller.ReportWiring();

        if (eventButtons.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("<b>Raise a world beat</b>", Bold(style));

            for (int i = 0; i < eventButtons.Count; i++)
            {
                EventButton e = eventButtons[i];
                if (e == null || e.channel == null)
                    continue;

                string label = string.IsNullOrEmpty(e.label) ? e.channel.name : e.label;
                if (GUILayout.Button(label, button))
                    e.channel.Raise();
            }
        }

        GUILayout.Space(10);
        GUILayout.Label($"[{toggleKey}] hides this panel. Click props directly to trigger them in the editor.", style);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private static GUIStyle Bold(GUIStyle from) => new GUIStyle(from) { richText = true };
}
