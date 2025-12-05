using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ScenarioManager : MonoBehaviour
{
    [SerializeField] List<QuestionSO> questions;
    [SerializeField] ResultsUI resultsUI;

    [Header("Feedback UI")]
    [SerializeField] GameObject feedbackPanel;                 // small panel that shows per-answer feedback
    [SerializeField] TextMeshProUGUI feedbackText;
    [SerializeField] TextMeshProUGUI severityText;
    [SerializeField] float feedbackDisplaySeconds = 10f;

    // Optional: reference to the quiz so we can enable/disable buttons
    [SerializeField] Quiz quiz;

    int currentIndex = 0;

    List<bool> answerCorrectness = new List<bool>();
    List<string> wrongFeedback = new List<string>();
    List<Severity> wrongSeverities = new List<Severity>();

    void Start()
    {
        if (quiz == null)
            quiz = FindObjectOfType<Quiz>();

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        LoadCurrentQuestion();
    }

    void LoadCurrentQuestion()
    {
        if (currentIndex < questions.Count)
        {
            quiz.ShowQuestion(questions[currentIndex]);
        }
        else
        {
            ShowFinalResults();
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
        bool correct = selectedIndex == q.GetCorrectAnswer();
        string fb = q.GetFeedback(selectedIndex);
        Severity sev = q.GetSeverity(selectedIndex);

        // Show feedback UI and after delay, record and proceed
        StartCoroutine(ShowFeedbackThenContinue(correct, fb, sev));
    }

    IEnumerator ShowFeedbackThenContinue(bool correct, string feedback, Severity sev)
    {
        // Display the feedback UI
        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        if (feedbackText != null)
            feedbackText.text = feedback ?? "";

        if (severityText != null)
            severityText.text = "Severity: " + sev.ToString();

        // Optional: set severity color or icon here (not implemented, but see notes below)

        // Wait for configured seconds (10s by default)
        yield return new WaitForSeconds(feedbackDisplaySeconds);

        // Hide feedback panel
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        // Record answer
        answerCorrectness.Add(correct);
        if (!correct)
        {
            wrongFeedback.Add(feedback);
            wrongSeverities.Add(sev);
        }

        // Advance index and re-enable inputs
        currentIndex++;
        if (quiz != null)
            quiz.SetButtonsInteractable(true);

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
        Severity maxSeverity = Severity.Mild;
        foreach (var sev in wrongSeverities)
        {
            if (sev > maxSeverity)
                maxSeverity = sev;
        }

        result += "Severity: **" + maxSeverity + "**\n\n";
        result += "Issues detected:\n";

        for (int i = 0; i < wrongFeedback.Count; i++)
        {
            result += "• " + wrongFeedback[i] + "\n";
        }

        resultsUI.ShowResult(result);
    }
}
