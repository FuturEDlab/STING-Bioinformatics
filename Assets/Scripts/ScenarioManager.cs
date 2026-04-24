using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ScenarioManager : MonoBehaviour
{
    [SerializeField] List<QuestionSO> generalQuestions;
    [SerializeField] List<QuestionSO> nursingQuestions;
    [SerializeField] List<QuestionSO> informaticsQuestions;
    [SerializeField] ResultsUI resultsUI;

    [Header("Feedback UI")]
    [SerializeField] GameObject feedbackPanel; // small panel that shows per-answer feedback
    [SerializeField] TextMeshProUGUI feedbackText;
    [SerializeField] float feedbackDisplaySeconds = 10f;

    // Optional: reference to the quiz so we can enable/disable buttons
    [SerializeField] Quiz quiz;
    
    private string questionType = "general";
    private string chosenMajor;
    int currentIndex = 0;
    // TODO: Uncomment 'severityText' variable to re-enable severity feature.
    // private string severityText;

    Dictionary<string, int> questionIndices = new Dictionary<string, int>();
    List<QuestionSO> questions;
    List<bool> answerCorrectness = new List<bool>();
    List<string> wrongFeedback = new List<string>();
    // TODO: Uncomment 'wrongSeverities' variable to re-enable severity feature.
    // List<Severity> wrongSeverities = new List<Severity>();

    void Start()
    {
        if (quiz == null)
            quiz = FindObjectOfType<Quiz>();

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        questionIndices[questionType] = 0;
        LoadCurrentQuestion();
    }

    void LoadCurrentQuestion()
    {
        if (questionType == "general")
            questions = generalQuestions;
        else if (questionType == "Nursing")
            questions = nursingQuestions;
        else if (questionType.Contains("Informatics"))
            questions = informaticsQuestions;
        else {ShowFinalResults(); return;}

        currentIndex = questionIndices[questionType];
        
        if (currentIndex < questions.Count)
        {
            quiz.ShowQuestion(questions[currentIndex]);
        }
    }

    // Called by Quiz when an answer button is clicked
    public void OnAnswerSelected(int selectedIndex)
    {
        // Immediately lock inputs
        if (quiz != null)
            quiz.SetButtonsInteractable(false);

        // Fetch data for display
        QuestionSO q = questions[currentIndex];

        if (q.name.Contains("SelectMajor_Q"))
        {
            chosenMajor = q.GetAnswer(selectedIndex);
        }

        bool correct = selectedIndex == q.GetCorrectAnswer();
        if (q.GetCorrectAnswer() == null)
        {
            correct = true;
        }
        
        string fb = q.GetFeedback(selectedIndex);
        // TODO: Uncomment 'sev' variable to re-enable severity feature.
        // Severity sev = q.GetSeverity(selectedIndex);

        // Show feedback UI and after delay, record and proceed
        // TODO: add 'sev' variable as a 3rd parameter in 'ShowFeedbackThenContinue' ->
        // to re-enable severity feature.
        StartCoroutine(ShowFeedbackThenContinue(correct, fb));
    }

    // TODO: uncomment 3rd parameter ('sev') in 'ShowFeedbackThenContinue' ->
    // IEnumerator/function to re-enable severity feature.
    IEnumerator ShowFeedbackThenContinue(bool correct, string feedback /* ,Severity sev */)
    {
        // TODO: add 'sev' variable as a 2nd parameter in 'DisplayFeedback' call to ->
        // re-enable severity feature.
        DisplayFeedback(feedback);
        // Optional: set severity color or icon here (not implemented, but see notes below)

        // Wait for configured seconds (10s by default)
        yield return new WaitForSeconds(feedbackDisplaySeconds);
        
        // TODO: add 'sev' variable as a 3rd parameter in 'ProgressQuiz' call to ->
        // re-enable severity feature.
        ProgressQuiz(correct, feedback);
    }

    // TODO: uncomment 2nd parameter ('sev') in 'DisplayFeedback' ->
    // function to re-enable severity feature.
    void DisplayFeedback(string feedback /* ,Severity sev */)
    {
        // TODO: uncomment 'severityText' variable to re-enable severity feature.
        // severityText = "Severity: " + sev;
        
        // Display the feedback UI
        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        if (feedbackText != null)
            feedbackText.text = feedback ?? "";

        // TODO: uncomment the 3 conditions below to re-enable severity feature.
        // if (sev == Severity.None)
        // {
        //     severityText = "";
        // }
        //
        // if (feedbackText.text.Length > 0)
        // {
        //     feedbackText.text = $"{feedbackText.text}\n\n{severityText}";
        // }
        // else
        // {
        //     feedbackText.text = $"{severityText}";
        // }
    }
    
    // TODO: uncomment 3rd parameter ('sev') in 'ProgressQuiz' ->
    // function to re-enable severity feature.
    void ProgressQuiz(bool correct, string feedback /* ,Severity sev */)
    {
        // Hide feedback panel
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        // Record answer
        answerCorrectness.Add(correct);
        if (!correct)
        {
            wrongFeedback.Add(feedback);
            // TODO: uncomment the 1 line below to re-enable severity feature.
            // wrongSeverities.Add(sev);
        }

        // Advance index and re-enable inputs
        questionIndices[questionType]++;
        currentIndex = questionIndices[questionType];
        // currentIndex++;
        if (quiz != null)
            quiz.SetButtonsInteractable(true);
        
        if (currentIndex >= questions.Count && questionType == chosenMajor)
        {
            questionType = "";
        }
        else if (currentIndex >= questions.Count && questionType.Contains("general"))
        {
            questionType = chosenMajor;
            questionIndices[questionType] = 0;
        }
        
        LoadCurrentQuestion();
    }

    void ShowFinalResults()
    {
        // Case 1: All safe
        if (!answerCorrectness.Contains(false))
        {
            resultsUI.ShowResult(
                "Safe to administer.\nAll selections matched safe clinical practice."
            );
            return;
        }

        // Case 2: Some mistakes → detailed harm summary
        string result = "Patient safety risk detected.\n\n";

        // Compute the highest severity
        // TODO: uncomment 'maxSeverity' variable and the foreach loop below to re-enable severity feature.
        // Severity maxSeverity = Severity.Mild;
        // foreach (var sev in wrongSeverities)
        // {
        //     if (sev > maxSeverity)
        //         maxSeverity = sev;
        // }

        // TODO: uncomment the 1 line below to re-enable severity feature.
        // result += "Severity: **" + maxSeverity + "**\n\n";
        result += "Issues detected:\n\n";

        for (int i = 0; i < wrongFeedback.Count; i++)
        {
            if (wrongFeedback[i].Length <= 0) continue;
            result += "• " + wrongFeedback[i] + "\n";
        }

        resultsUI.ShowResult(result);
    }
}
