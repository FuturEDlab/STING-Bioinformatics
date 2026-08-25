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

    [Tooltip("Silence inserted between the phrases of one VO line. 0 plays them back to back.")]
    [SerializeField] private float gapBetweenPhrases = 0f;

    [Tooltip("Scene surface that shows the caption of the phrase currently playing. Optional — captions are simply skipped when empty.")]
    [SerializeField] private CaptionDisplay captionDisplay;

    [Header("Highlighting")]
    [Tooltip("Broadcasts the task id the scenario is currently waiting on (empty string when it is not waiting). Highlightable objects listen here so only the object the player needs right now glows.")]
    [SerializeField] private StringGameEvent focusChannel;

    [Header("Question panel (the image-based Question Panels prefab)")]
    [Tooltip("The QuestionPanelManager in the scene. Used by Panel Question steps for the in-simulation quiz, and it also runs the Scene 4 assessment.")]
    [SerializeField] private QuestionPanelManager questionPanel;

    [Header("Legacy text quiz — one scene panel per question asset")]
    [Tooltip("Each row maps a question asset to the scene panel that shows it. A question with no row here falls back to the single shared Quiz / PC Ui Root below.")]
    [SerializeField] private List<QuestionPanelBinding> questionPanels = new List<QuestionPanelBinding>();

    [Header("Teleport destinations — one scene Transform per teleport step asset")]
    [SerializeField] private List<TeleportDestinationBinding> teleportDestinations = new List<TeleportDestinationBinding>();

    [Header("Fallback single-panel Question / PC UI (used only by questions with no panel above)")]
    [SerializeField] private Quiz quiz;
    [SerializeField] private GameObject pcUiRoot;   // quiz canvas/panel root, toggled with SetActive
    [SerializeField] private ResultsUI resultsUI;   // optional end screen

    [Header("Player / XR Rig — no longer required, kept so old scenes keep their wiring")]
    [Tooltip("Legacy. Teleport steps and everything else now find the player through Rig, which resolves either the BNG army-guy rig or the new VR Player hands on its own. Leave empty in new scenes.")]
    [SerializeField] private BNG.BNGPlayerController player;

    [Tooltip("Legacy. See above — nothing reads this any more.")]
    [SerializeField] private Transform playerRig;

    [Tooltip("Legacy. Screen fades go through Rig now, which uses BNG's ScreenFader on the old rig and ScreenFade on the new one. Leave empty in new scenes.")]
    [SerializeField] private BNG.ScreenFader screenFader;

    public AudioSource VoSource => voSource;
    public CaptionDisplay CaptionDisplay => captionDisplay;
    public StringGameEvent FocusChannel => focusChannel;
    public QuestionPanelManager QuestionPanel => questionPanel;
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

    /// <summary>
    /// Announce which gameplay task the scenario is now waiting on, so the matching
    /// object can glow. Pass null/empty to clear. Safe to call when no channel is wired.
    /// </summary>
    public void SetFocus(string taskId)
    {
        if (focusChannel != null)
            focusChannel.Raise(taskId ?? string.Empty);
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
    /// The ONE audio path used by every step (NarratorStep + UIStep feedback). Recordings
    /// are authored as several short phrases, so a line is a LIST of phrases: they play
    /// back to back, each showing its caption, and <paramref name="onFinished"/> fires
    /// once the last one ends. An empty list (or a missing runner) completes immediately,
    /// and empty slots inside a list are skipped rather than stalling the step. A phrase
    /// whose recording is missing but that has a caption holds the caption for a
    /// reading-speed duration instead, so unrecorded lines still carry the scenario.
    /// </summary>
    public void PlayVoice(IReadOnlyList<CaptionedClip> phrases, Action onFinished)
    {
        // Cancel any pending wait so an interrupted VO never fires a stale callback.
        StopVoiceRoutine();

        if (Runner == null || !HasAnyContent(phrases))
        {
            onFinished?.Invoke();
            return;
        }

        voRoutine = Runner.StartCoroutine(PlayPhrases(phrases, onFinished));
    }

    public void StopVoice()
    {
        StopVoiceRoutine();
        if (voSource != null)
            voSource.Stop();
        if (captionDisplay != null)
            captionDisplay.Hide();
    }

    private void StopVoiceRoutine()
    {
        if (voRoutine != null && Runner != null)
            Runner.StopCoroutine(voRoutine);
        voRoutine = null;
    }

    private static bool HasAnyContent(IReadOnlyList<CaptionedClip> phrases)
    {
        if (phrases == null)
            return false;

        for (int i = 0; i < phrases.Count; i++)
        {
            if (phrases[i] != null && phrases[i].HasContent)
                return true;
        }

        return false;
    }

    private IEnumerator PlayPhrases(IReadOnlyList<CaptionedClip> phrases, Action done)
    {
        for (int i = 0; i < phrases.Count; i++)
        {
            CaptionedClip phrase = phrases[i];
            if (phrase == null || !phrase.HasContent)
                continue;

            // A phrase without its own caption leaves the previous blob on screen rather
            // than blanking it, so a caption that spans two clips stays readable.
            if (captionDisplay != null && phrase.HasCaption)
                captionDisplay.Show(phrase);

            if (phrase.Clip != null && voSource != null)
            {
                voSource.Stop();
                voSource.clip = phrase.Clip;
                voSource.Play();
                yield return new WaitForSeconds(phrase.Clip.length);
            }
            else
            {
                // Recording missing (or no source): hold the caption long enough to read.
                yield return new WaitForSeconds(phrase.FallbackSeconds);
            }

            if (gapBetweenPhrases > 0f && i < phrases.Count - 1)
                yield return new WaitForSeconds(gapBetweenPhrases);
        }

        if (captionDisplay != null)
            captionDisplay.Hide();

        voRoutine = null;
        done?.Invoke();
    }
}
