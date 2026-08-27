using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Asks one question through the project's <see cref="QuestionPanelManager"/> prefab — the
/// image-based panel that also runs the post-experience assessment — and speaks a
/// different narrator line depending on which answer the player picks.
///
/// This is the in-simulation quiz ("[SIMULATION PAUSE - QUIZ]" in the script). It exists
/// alongside <see cref="UIQuestionStepData"/>, which drives the older text-based
/// <see cref="Quiz"/> panel; use whichever matches the panel in your scene.
///
/// The question itself comes from the shared <see cref="QuestionBank"/> asset, so the
/// wording and answer artwork are authored in exactly one place.
/// </summary>
[CreateAssetMenu(fileName = "PanelQuestionStep", menuName = "Scenario/Steps/Panel Question")]
public class PanelQuestionStepData : ScenarioStepData
{
    public enum Source
    {
        [InspectorName("Universal question")] Universal,
        [InspectorName("Major-specific question")] Major,
    }

    [Header("Question")]
    [Tooltip("The bank holding the question — normally BioQuestions.")]
    [SerializeField] private QuestionBank bank;

    [Tooltip("Which question in the bank to ask.")]
    [SerializeField] private Source source = Source.Universal;

    [Tooltip("Index into the bank's Major Question Sets. Only used when Source is Major.")]
    [SerializeField] private int majorIndex;

    [Header("Prompt VO (optional; plays as the panel appears)")]
    [SerializeField] private List<CaptionedClip> questionVo = new List<CaptionedClip>();

    [Header("Per-answer narrator feedback (index-aligned with the question's answers)")]
    [Tooltip("One line per answer. An empty entry falls back to the generic correct/wrong line below.")]
    [SerializeField] private List<VoiceLine> perAnswerFeedbackVo = new List<VoiceLine>();

    [Header("Generic feedback fallback")]
    [SerializeField] private List<CaptionedClip> correctFeedbackVo = new List<CaptionedClip>();
    [SerializeField] private List<CaptionedClip> wrongFeedbackVo = new List<CaptionedClip>();

    [Header("Retry policy")]
    [Tooltip("Attempts before the step gives up. 0 or negative = unlimited (must answer correctly to proceed).")]
    [SerializeField] private int allowedTries = 0;

    [Tooltip("When allowedTries is reached without a correct answer: if true, move on anyway.")]
    [SerializeField] private bool advanceOnFail = false;

    public QuestionBank Bank => bank;
    public IReadOnlyList<CaptionedClip> QuestionVo => questionVo;
    public int AllowedTries => allowedTries;
    public bool AdvanceOnFail => advanceOnFail;

    /// <summary>The QuestionData this step asks, or null when the bank is missing/misindexed.</summary>
    public QuestionData ResolveQuestion()
    {
        if (bank == null)
            return null;

        if (source == Source.Universal)
            return bank.universalQuestion;

        if (bank.majorQuestionSets == null || majorIndex < 0 || majorIndex >= bank.majorQuestionSets.Length)
            return null;

        return bank.majorQuestionSets[majorIndex]?.majorQuestion;
    }

    /// <summary>Feedback for <paramref name="answerIndex"/>, falling back to the generic lines.</summary>
    public IReadOnlyList<CaptionedClip> GetFeedbackVo(int answerIndex, bool isCorrect)
    {
        if (answerIndex >= 0 && answerIndex < perAnswerFeedbackVo.Count)
        {
            VoiceLine line = perAnswerFeedbackVo[answerIndex];
            if (line != null && line.HasContent)
                return line.Phrases;
        }

        return isCorrect ? correctFeedbackVo : wrongFeedbackVo;
    }

    public override IScenarioStep CreateRuntimeStep() => new PanelQuestionStep(this);
}

/// <summary>Runtime executor for <see cref="PanelQuestionStepData"/>.</summary>
public class PanelQuestionStep : IScenarioStep
{
    private readonly PanelQuestionStepData data;
    private ScenarioContext ctx;
    private Action onComplete;
    private QuestionPanelManager panel;
    private int triesUsed;
    private bool subscribed;
    private bool completed;

    public PanelQuestionStep(PanelQuestionStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext ctx, Action onComplete)
    {
        this.ctx = ctx;
        this.onComplete = onComplete;
        triesUsed = 0;
        completed = false;

        panel = ctx.QuestionPanel;

        if (panel != null && !panel.showSimulationQuestions)
        {
            Debug.Log($"[PanelQuestionStep] '{data.name}': simulation questions are disabled. Continuing without opening the question panel.", panel);
            onComplete?.Invoke();
            return;
        }

        QuestionData question = data.ResolveQuestion();

        if (panel == null)
        {
            Debug.LogError($"[PanelQuestionStep] '{data.name}': no Question Panel assigned under Context ▸ Question Panel on the ScenarioController. Skipping the question.");
            onComplete?.Invoke();
            return;
        }

        if (question == null)
        {
            Debug.LogError($"[PanelQuestionStep] '{data.name}': could not resolve a question from the bank. Check the Bank and Major Index fields. Skipping.");
            onComplete?.Invoke();
            return;
        }

        Subscribe();
        panel.ShowSingleQuestion(question);

        Debug.Log($"[PanelQuestionStep] Showing \"{question.questionText}\" on panel '{panel.name}'. If you cannot see it, the panel is in the scene but positioned out of view, or its Canvas is not set up for VR.", panel);

        // Prompt VO runs alongside the visible panel rather than gating it.
        ctx.PlayVoice(data.QuestionVo, null);
    }

    private void Subscribe()
    {
        if (!subscribed && panel != null)
        {
            panel.AnswerConfirmed += OnAnswerConfirmed;
            subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (subscribed && panel != null)
        {
            panel.AnswerConfirmed -= OnAnswerConfirmed;
            subscribed = false;
        }
    }

    private void OnAnswerConfirmed(int answerIndex, bool isCorrect)
    {
        // The panel has already locked its buttons and shown the explanation image; the
        // narration plays over that, and we decide what happens when it ends.
        IReadOnlyList<CaptionedClip> feedback = data.GetFeedbackVo(answerIndex, isCorrect);
        ctx.PlayVoice(feedback, () => OnFeedbackDone(isCorrect));
    }

    private void OnFeedbackDone(bool isCorrect)
    {
        if (isCorrect)
        {
            Complete();
            return;
        }

        triesUsed++;

        bool unlimited = data.AllowedTries <= 0;
        bool triesLeft = unlimited || triesUsed < data.AllowedTries;

        if (triesLeft || !data.AdvanceOnFail)
        {
            panel.ReaskCurrentQuestion();
            return;
        }

        Complete();
    }

    private void Complete()
    {
        if (completed)
            return;
        completed = true;

        Unsubscribe();
        panel.ClosePanel();
        onComplete?.Invoke();
    }

    public void Exit()
    {
        Unsubscribe();
        ctx?.StopVoice();

        if (panel != null)
            panel.ClosePanel();
    }
}
