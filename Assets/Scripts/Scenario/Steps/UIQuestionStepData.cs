using System;
using UnityEngine;

/// <summary>
/// Shows one question on the PC screen, validates the answer against the QuestionSO,
/// plays feedback VO, and either completes or re-asks per the retry policy.
/// </summary>
[CreateAssetMenu(fileName = "UIQuestionStep", menuName = "Scenario/Steps/UI Question")]
public class UIQuestionStepData : ScenarioStepData
{
    [Header("Question")]
    public QuestionSO question;

    [Header("Retry policy")]
    [Min(1)]
    [Tooltip("Attempts the player gets. After the last wrong attempt the step completes anyway - " +
             "the scenario never dead-ends.")]
    public int maxTries = 1;

    [Header("Feedback voice over (optional)")]
    public AudioClip correctVo;
    public AudioClip incorrectVo;

    [Tooltip("Played instead of Incorrect VO on the final failed attempt, if assigned.")]
    public AudioClip outOfTriesVo;

    [Header("Panel")]
    [Tooltip("Hide ScenarioContext.PcUiRoot when this step ends. Turn off if the next step also uses the screen.")]
    public bool hidePanelOnExit = true;

    public override IScenarioStep CreateRuntimeStep() => new UIQuestionStep(this);
}

public class UIQuestionStep : IScenarioStep
{
    private readonly UIQuestionStepData data;
    private ScenarioContext ctx;
    private Action onComplete;

    private bool subscribed;
    private bool completed;
    private int triesUsed;

    public UIQuestionStep(UIQuestionStepData data)
    {
        this.data = data;
    }

    public void Enter(ScenarioContext context, Action onComplete)
    {
        ctx = context;
        this.onComplete = onComplete;
        triesUsed = 0;
        completed = false;

        if (ctx.Quiz == null || data.question == null)
        {
            Debug.LogError($"[Scenario] '{data.name}' needs a Quiz on ScenarioContext and a QuestionSO - skipping step.");
            Complete();
            return;
        }

        if (ctx.PcUiRoot != null)
            ctx.PcUiRoot.SetActive(true);

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (completed) return;

        // Subscribe only while the question is actually on screen.
        Subscribe();

        ctx.Quiz.ShowQuestion(data.question);
        ctx.Quiz.SetButtonsInteractable(true);
    }

    private void OnAnswerSelected(int selectedIndex)
    {
        // Lock the instant an answer arrives. Unsubscribing here is the re-entrancy guard:
        // a fast double-tap must not consume two tries.
        Unsubscribe();
        ctx.Quiz.SetButtonsInteractable(false);

        triesUsed++;

        // A null correct index means the question has no single right answer
        // (QuestionSO uses a non-numeric correctAnswerIndex for that), so anything passes.
        int? correctIndex = data.question.GetCorrectAnswer();
        bool correct = !correctIndex.HasValue || selectedIndex == correctIndex.Value;

        bool retry = !correct && triesUsed < data.maxTries;

        AudioClip vo;
        if (correct) vo = data.correctVo;
        else if (retry) vo = data.incorrectVo;
        else vo = data.outOfTriesVo != null ? data.outOfTriesVo : data.incorrectVo;

        // Same single audio path as narration, so feedback obeys the Narration mixer group too.
        Action next = retry ? (Action)ShowQuestion : (Action)Complete;
        ctx.PlayVoice(vo, next);
    }

    public void Exit()
    {
        Unsubscribe();

        if (ctx != null)
        {
            ctx.StopVoice();

            if (data.hidePanelOnExit && ctx.PcUiRoot != null)
                ctx.PcUiRoot.SetActive(false);
        }

        ctx = null;
    }

    private void Complete()
    {
        if (completed) return;
        completed = true;

        Unsubscribe();

        Action callback = onComplete;
        onComplete = null;
        callback?.Invoke();
    }

    private void Subscribe()
    {
        if (subscribed || ctx?.Quiz == null) return;

        subscribed = true;
        ctx.Quiz.AnswerSelected += OnAnswerSelected;
    }

    private void Unsubscribe()
    {
        if (!subscribed || ctx?.Quiz == null) return;

        subscribed = false;
        ctx.Quiz.AnswerSelected -= OnAnswerSelected;
    }
}
