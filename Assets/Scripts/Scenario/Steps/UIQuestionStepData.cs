using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data for a question / open-PC-UI step with answer validation, narrator feedback, and
/// a retry policy. Reuses the existing <see cref="QuestionSO"/> for the question content
/// and the existing <see cref="Quiz"/> UI for display.
/// </summary>
[CreateAssetMenu(fileName = "UIQuestionStep", menuName = "Scenario/Steps/UI Question")]
public class UIQuestionStepData : ScenarioStepData
{
    [SerializeField] private QuestionSO question;

    [Header("Question prompt VO (optional; plays as the panel appears)")]
    [Tooltip("Phrases of the prompt, played in order.")]
    [SerializeField] private List<AudioClip> questionVoClips = new List<AudioClip>();

    [Header("Narrator feedback (played through the shared VO path)")]
    [Tooltip("Phrases played after a correct answer, in order.")]
    [SerializeField] private List<AudioClip> correctFeedbackVoClips = new List<AudioClip>();

    [Tooltip("Phrases played after a wrong answer, in order.")]
    [SerializeField] private List<AudioClip> wrongFeedbackVoClips = new List<AudioClip>();

    [Header("Retry policy")]
    [Tooltip("Attempts before the step gives up. 0 or negative = unlimited (must answer correctly to proceed).")]
    [SerializeField] private int allowedTries = 0;
    [Tooltip("When allowedTries is reached without a correct answer: if true, complete and move on anyway.")]
    [SerializeField] private bool advanceOnFail = false;

    public QuestionSO Question => question;
    public IReadOnlyList<AudioClip> QuestionVoClips => questionVoClips;
    public IReadOnlyList<AudioClip> CorrectFeedbackVoClips => correctFeedbackVoClips;
    public IReadOnlyList<AudioClip> WrongFeedbackVoClips => wrongFeedbackVoClips;
    public int AllowedTries => allowedTries;
    public bool AdvanceOnFail => advanceOnFail;

    public override IScenarioStep CreateRuntimeStep() => new UIQuestionStep(this);
}

/// <summary>Runtime executor for <see cref="UIQuestionStepData"/>.</summary>
public class UIQuestionStep : IScenarioStep
{
    private readonly UIQuestionStepData data;
    private ScenarioContext ctx;
    private Action onComplete;
    private int triesUsed;
    private bool subscribed;
    private bool completed;

    // Resolved once per entry: either the scene panel bound to this question, or the
    // single shared panel from the context when the question has no binding.
    private QuestionPanel panel;
    private Quiz quiz;
    private GameObject fallbackRoot;

    public UIQuestionStep(UIQuestionStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext ctx, Action onComplete)
    {
        this.ctx = ctx;
        this.onComplete = onComplete;
        triesUsed = 0;
        completed = false;

        ResolveUi();

        if (quiz == null)
        {
            // A bound panel missing its Quiz is already reported by ResolveUi.
            if (panel == null)
            {
                string questionName = data.Question != null ? data.Question.name : "<none>";
                Debug.LogError($"[UIQuestionStep] Question '{questionName}' has no panel bound under Context ▸ Question Panels on the ScenarioController, and no fallback Quiz is assigned. Skipping the step.");
            }

            onComplete?.Invoke();
            return;
        }

        ShowPanel();
        ShowQuestion();
    }

    /// <summary>
    /// Step assets are ScriptableObjects and cannot hold scene references, so the panel
    /// for this question is looked up through the controller's Inspector bindings.
    /// </summary>
    private void ResolveUi()
    {
        panel = ctx.GetQuestionPanel(data.Question);

        if (panel != null)
        {
            quiz = panel.Quiz;
            fallbackRoot = null;

            if (quiz == null)
                Debug.LogError($"[UIQuestionStep] Panel '{panel.name}' has no Quiz assigned.", panel);

            return;
        }

        // Unbound question: fall back to the single shared quiz panel.
        quiz = ctx.Quiz;
        fallbackRoot = ctx.PcUiRoot;
    }

    private void ShowPanel()
    {
        if (panel != null)
            panel.Show();
        else if (fallbackRoot != null)
            fallbackRoot.SetActive(true);
    }

    private void HidePanel()
    {
        if (panel != null)
            panel.Hide();
        else if (fallbackRoot != null)
            fallbackRoot.SetActive(false);
    }

    private void ShowQuestion()
    {
        quiz.ShowQuestion(data.Question);
        quiz.SetButtonsInteractable(true);
        Subscribe();

        // Prompt VO runs alongside the visible panel rather than gating it. Answering
        // mid-clip is safe: PlayVoice cancels this pending wait before the feedback clip,
        // so no stale callback survives. Re-asking replays the prompt.
        ctx.PlayVoice(data.QuestionVoClips, null);
    }

    private void Subscribe()
    {
        if (!subscribed && quiz != null)
        {
            quiz.AnswerSelected += OnAnswer;
            subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (subscribed && quiz != null)
        {
            quiz.AnswerSelected -= OnAnswer;
            subscribed = false;
        }
    }

    private void OnAnswer(int index)
    {
        // Re-entrancy guard: stop listening and lock the buttons the instant an answer
        // arrives, so a fast double-answer can't consume two tries or double-complete.
        Unsubscribe();
        quiz.SetButtonsInteractable(false);

        int? correct = data.Question.GetCorrectAnswer();
        bool isCorrect = (correct == null) || (index == correct.Value);

        IReadOnlyList<AudioClip> feedback = isCorrect ? data.CorrectFeedbackVoClips : data.WrongFeedbackVoClips;
        // All feedback phrases finish before we re-show or complete (the callback fires
        // when the last one ends).
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

        if (triesLeft)
        {
            ShowQuestion(); // re-subscribes and re-enables buttons
            return;
        }

        // No tries remain.
        if (data.AdvanceOnFail)
            Complete();
        else
            ShowQuestion(); // bounded but must-be-correct: keep asking until correct
    }

    private void Complete()
    {
        if (completed)
            return;
        completed = true;

        Unsubscribe();
        HidePanel();
        onComplete?.Invoke();
    }

    public void Exit()
    {
        Unsubscribe();
        ctx?.StopVoice();
        HidePanel();
    }
}
