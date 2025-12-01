using UnityEngine;

public enum Severity
{
    Safe,
    Mild,
    Moderate,
    Critical
}

[CreateAssetMenu(fileName = "New Question", menuName = "Quiz Question")]
public class QuestionSO : ScriptableObject
{
    [TextArea(2, 6)]
    [SerializeField] string question = "Enter new question here";

    [SerializeField] string[] answers = new string[4];

    [Header("Index of the safe/correct answer")]
    [SerializeField] int correctAnswerIndex;

    [Header("Feedback shown ONLY at the end (same size as Answers)")]
    [TextArea(2, 6)]
    [SerializeField] string[] feedback = new string[4];

    [Header("Severity of each answer (same size as Answers)")]
    [SerializeField] Severity[] severities = new Severity[4];

    // Public Getters
    

    public string GetQuestion()
    {
        return question;
    }

    public string GetAnswer(int index)
    {
        return answers[index];
    }

    public int GetAnswerCount()
    {
        return answers.Length;
    }

    public int GetCorrectAnswer()
    {
        return correctAnswerIndex;
    }

    public string GetFeedback(int index)
    {
        // Safety check
        if (feedback == null || index < 0 || index >= feedback.Length)
            return "";
        return feedback[index];
    }

    public Severity GetSeverity(int index)
    {
        // Default safe if data missing
        if (severities == null || index < 0 || index >= severities.Length)
            return Severity.Safe;
        return severities[index];
    }
}