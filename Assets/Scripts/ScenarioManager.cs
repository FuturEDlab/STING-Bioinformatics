using UnityEngine;
using System.Collections.Generic;

public class ScenarioManager : MonoBehaviour
{
    [SerializeField] List<QuestionSO> questions;
    [SerializeField] ResultsUI resultsUI; 

    int currentIndex = 0;

    List<bool> answerCorrectness = new List<bool>();
    List<string> wrongFeedback = new List<string>();
    List<Severity> wrongSeverities = new List<Severity>();

    Quiz quiz;

    void Start()
    {
        quiz = FindObjectOfType<Quiz>();
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

    public void OnAnswerSelected(int index)
    {
        QuestionSO q = questions[currentIndex];
        bool correct = index == q.GetCorrectAnswer();
        answerCorrectness.Add(correct);

        if (!correct)
        {
            wrongFeedback.Add(q.GetFeedback(index));
            wrongSeverities.Add(q.GetSeverity(index));
        }

        currentIndex++;
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
