using System;
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
    [SerializeField] private AudioClip questionVo;

    [Header("Narrator feedback (played through the shared VO path)")]
    [SerializeField] private AudioClip correctFeedbackVo;
    [SerializeField] private AudioClip wrongFeedbackVo;

    [Header("Retry policy")]
    [Tooltip("Attempts before the step gives up. 0 or negative = unlimited (must answer correctly to proceed).")]
    [SerializeField] private int allowedTries = 0;
    [Tooltip("When allowedTries is reached without a correct answer: if true, complete and move on anyway.")]
    [SerializeField] private bool advanceOnFail = false;

    public QuestionSO Question => question;
    public AudioClip QuestionVo => questionVo;
    public AudioClip CorrectFeedbackVo => correctFeedbackVo;
    public AudioClip WrongFeedbackVo => wrongFeedbackVo;
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

        if (ctx.PcUiRoot != null)
            ctx.PcUiRoot.SetActive(true);

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        ctx.Quiz.ShowQuestion(data.Question);
        ctx.Quiz.SetButtonsInteractable(true);
        Subscribe();

        // Prompt VO runs alongside the visible panel rather than gating it. Answering
        // mid-clip is safe: PlayVoice cancels this pending wait before the feedback clip,
        // so no stale callback survives. Re-asking replays the prompt.
        ctx.PlayVoice(data.QuestionVo, null);
    }

    private void Subscribe()
    {
        if (!subscribed)
        {
            ctx.Quiz.AnswerSelected += OnAnswer;
            subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (subscribed)
        {
            ctx.Quiz.AnswerSelected -= OnAnswer;
            subscribed = false;
        }
    }

    private void OnAnswer(int index)
    {
        // Re-entrancy guard: stop listening and lock the buttons the instant an answer
        // arrives, so a fast double-answer can't consume two tries or double-complete.
        Unsubscribe();
        ctx.Quiz.SetButtonsInteractable(false);

        int? correct = data.Question.GetCorrectAnswer();
        bool isCorrect = (correct == null) || (index == correct.Value);

        AudioClip fb = isCorrect ? data.CorrectFeedbackVo : data.WrongFeedbackVo;
        // Feedback finishes before we re-show or complete (callback fires when VO ends).
        ctx.PlayVoice(fb, () => OnFeedbackDone(isCorrect));
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
        if (ctx.PcUiRoot != null)
            ctx.PcUiRoot.SetActive(false);
        onComplete?.Invoke();
    }

    public void Exit()
    {
        Unsubscribe();
        ctx?.StopVoice();
        if (ctx != null && ctx.PcUiRoot != null)
            ctx.PcUiRoot.SetActive(false);
    }
}
