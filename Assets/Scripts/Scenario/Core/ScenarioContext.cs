using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds one question asset to the scene panel that presents it. Needed because step
/// assets are ScriptableObjects and cannot reference scene objects — the binding lives
/// on the ScenarioController instead.
/// </summary>
[Serializable]
public class QuestionPanelBinding
{
    [Tooltip("The question asset this panel presents.")]
    [SerializeField] private QuestionSO question;

    [Tooltip("The scene panel (QuestionPanel component) that shows this question.")]
    [SerializeField] private QuestionPanel panel;

    public QuestionSO Question => question;
    public QuestionPanel Panel => panel;

    public QuestionPanelBinding() { }

    public QuestionPanelBinding(QuestionSO question)
    {
        this.question = question;
    }
}

/// <summary>
/// Binds one teleport step asset to the scene Transform the player is moved to. Same
/// reason as <see cref="QuestionPanelBinding"/>: the destination is a scene object.
/// </summary>
[Serializable]
public class TeleportDestinationBinding
{
    [Tooltip("The teleport step asset this destination belongs to.")]
    [SerializeField] private TeleportStepData step;

    [Tooltip("Scene Transform the player is moved to (position, plus rotation if the step matches it).")]
    [SerializeField] private Transform destination;

    public TeleportStepData Step => step;
    public Transform Destination => destination;

    public TeleportDestinationBinding() { }

    public TeleportDestinationBinding(TeleportStepData step)
    {
        this.step = step;
    }
}

/// <summary>
/// Small serialized bag of scene references the steps need, plus the single shared
/// voice-over audio path. Wired on the ScenarioController in the Inspector.
/// </summary>
[Serializable]
public class ScenarioContext
{
    [Header("Voice-over (route this AudioSource's output to the Narration mixer group)")]
    [SerializeField] private AudioSource voSource;

    [Header("Question panels — one scene panel per question asset")]
    [Tooltip("Each row maps a question asset to the scene panel that shows it. A question with no row here falls back to the single shared Quiz / PC Ui Root below.")]
    [SerializeField] private List<QuestionPanelBinding> questionPanels = new List<QuestionPanelBinding>();

    [Header("Teleport destinations — one scene Transform per teleport step asset")]
    [SerializeField] private List<TeleportDestinationBinding> teleportDestinations = new List<TeleportDestinationBinding>();

    [Header("Fallback single-panel Question / PC UI (used only by questions with no panel above)")]
    [SerializeField] private Quiz quiz;
    [SerializeField] private GameObject pcUiRoot;   // quiz canvas/panel root, toggled with SetActive
    [SerializeField] private ResultsUI resultsUI;   // optional end screen

    [Header("Player / XR Rig (optional)")]
    [SerializeField] private BNG.BNGPlayerController player;
    [SerializeField] private Transform playerRig;

    [Tooltip("Optional. Left empty, it is looked up under the player's CameraRig the first time a teleport step needs it.")]
    [SerializeField] private BNG.ScreenFader screenFader;

    public AudioSource VoSource => voSource;
    public Quiz Quiz => quiz;
    public GameObject PcUiRoot => pcUiRoot;
    public ResultsUI ResultsUI => resultsUI;
    public BNG.BNGPlayerController Player => player;
    public Transform PlayerRig => playerRig;
    public IReadOnlyList<QuestionPanelBinding> QuestionPanels => questionPanels;
    public IReadOnlyList<TeleportDestinationBinding> TeleportDestinations => teleportDestinations;

    /// <summary>Injected at runtime by ScenarioController so the context can run coroutines.</summary>
    public MonoBehaviour Runner { get; set; }

    private Coroutine voRoutine;

    // Lookups are built on first use and dropped by InvalidateLookups(), so Inspector
    // edits made between runs are always picked up.
    private Dictionary<QuestionSO, QuestionPanel> panelLookup;
    private Dictionary<TeleportStepData, Transform> destinationLookup;
    private bool faderSearched;

    /// <summary>
    /// The scene panel bound to <paramref name="question"/>, or null when that question has
    /// no row in the Inspector list.
    /// </summary>
    public QuestionPanel GetQuestionPanel(QuestionSO question)
    {
        if (question == null)
            return null;

        if (panelLookup == null)
            BuildPanelLookup();

        return panelLookup.TryGetValue(question, out QuestionPanel panel) ? panel : null;
    }

    /// <summary>The scene Transform bound to <paramref name="step"/>, or null when unbound.</summary>
    public Transform GetTeleportDestination(TeleportStepData step)
    {
        if (step == null)
            return null;

        if (destinationLookup == null)
            BuildDestinationLookup();

        return destinationLookup.TryGetValue(step, out Transform destination) ? destination : null;
    }

    /// <summary>
    /// Switches every bound panel off. Called when the scenario begins so a panel left
    /// visible in the scene never sits on top of the question actually being asked.
    /// </summary>
    public void HideAllQuestionPanels()
    {
        for (int i = 0; i < questionPanels.Count; i++)
        {
            QuestionPanel panel = questionPanels[i]?.Panel;
            if (panel != null)
                panel.Hide();
        }
    }

    /// <summary>Drops the cached lookups so the next access re-reads the Inspector lists.</summary>
    public void InvalidateLookups()
    {
        panelLookup = null;
        destinationLookup = null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: adds an empty row for <paramref name="question"/> when it has none, so
    /// the designer only has to drag the scene panel in. Returns true when a row was added.
    /// </summary>
    public bool EnsureQuestionRow(QuestionSO question)
    {
        if (question == null)
            return false;

        for (int i = 0; i < questionPanels.Count; i++)
        {
            if (questionPanels[i] != null && questionPanels[i].Question == question)
                return false;
        }

        questionPanels.Add(new QuestionPanelBinding(question));
        InvalidateLookups();
        return true;
    }

    /// <summary>Editor-only counterpart of <see cref="EnsureQuestionRow"/> for teleport steps.</summary>
    public bool EnsureTeleportRow(TeleportStepData step)
    {
        if (step == null)
            return false;

        for (int i = 0; i < teleportDestinations.Count; i++)
        {
            if (teleportDestinations[i] != null && teleportDestinations[i].Step == step)
                return false;
        }

        teleportDestinations.Add(new TeleportDestinationBinding(step));
        InvalidateLookups();
        return true;
    }
#endif

    /// <summary>
    /// The screen fader used by teleport steps. Resolved from the player's CameraRig when
    /// not assigned — a scoped lookup, never a global search.
    /// </summary>
    public BNG.ScreenFader ScreenFader
    {
        get
        {
            if (screenFader == null && !faderSearched)
            {
                faderSearched = true;
                if (player != null && player.CameraRig != null)
                    screenFader = player.CameraRig.GetComponentInChildren<BNG.ScreenFader>(true);
            }
            return screenFader;
        }
    }

    private void BuildPanelLookup()
    {
        panelLookup = new Dictionary<QuestionSO, QuestionPanel>();

        for (int i = 0; i < questionPanels.Count; i++)
        {
            QuestionPanelBinding binding = questionPanels[i];
            if (binding == null || binding.Question == null)
                continue;

            if (panelLookup.ContainsKey(binding.Question))
            {
                Debug.LogWarning($"[ScenarioContext] Question '{binding.Question.name}' is bound to more than one panel; the first row wins.");
                continue;
            }

            if (binding.Panel == null)
            {
                Debug.LogWarning($"[ScenarioContext] Question '{binding.Question.name}' has a row with no panel assigned.");
                continue;
            }

            panelLookup.Add(binding.Question, binding.Panel);
        }
    }

    private void BuildDestinationLookup()
    {
        destinationLookup = new Dictionary<TeleportStepData, Transform>();

        for (int i = 0; i < teleportDestinations.Count; i++)
        {
            TeleportDestinationBinding binding = teleportDestinations[i];
            if (binding == null || binding.Step == null)
                continue;

            if (destinationLookup.ContainsKey(binding.Step))
            {
                Debug.LogWarning($"[ScenarioContext] Teleport step '{binding.Step.name}' is bound to more than one destination; the first row wins.");
                continue;
            }

            if (binding.Destination == null)
            {
                Debug.LogWarning($"[ScenarioContext] Teleport step '{binding.Step.name}' has a row with no destination assigned.");
                continue;
            }

            destinationLookup.Add(binding.Step, binding.Destination);
        }
    }

    /// <summary>
    /// The ONE audio path used by every step (NarratorStep + UIStep feedback). Plays a
    /// clip and invokes <paramref name="onFinished"/> when it ends. A null clip (or a
    /// missing source/runner) completes immediately.
    /// </summary>
    public void PlayVoice(AudioClip clip, Action onFinished)
    {
        // Cancel any pending wait so an interrupted VO never fires a stale callback.
        if (voRoutine != null && Runner != null)
        {
            Runner.StopCoroutine(voRoutine);
            voRoutine = null;
        }

        if (clip == null || voSource == null || Runner == null)
        {
            onFinished?.Invoke();
            return;
        }

        voSource.Stop();
        voSource.clip = clip;
        voSource.Play();
        voRoutine = Runner.StartCoroutine(WaitVoice(clip.length, onFinished));
    }

    public void StopVoice()
    {
        if (voRoutine != null && Runner != null)
        {
            Runner.StopCoroutine(voRoutine);
            voRoutine = null;
        }
        if (voSource != null)
            voSource.Stop();
    }

    private IEnumerator WaitVoice(float seconds, Action done)
    {
        yield return new WaitForSeconds(seconds);
        voRoutine = null;
        done?.Invoke();
    }
}
