using UnityEngine;

public enum Severity
{
    None,
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
    [Tooltip("To make all answers right instead of just one answer: insert non-integer, Ex: All")]
    [SerializeField] string correctAnswerIndex;

    [Header("Feedback shown ONLY at the end (same size as Answers)")]
    [TextArea(2, 6)]
    [SerializeField] string[] feedback = new string[4];

    // TODO: Uncomment the 2 lines below to re-enable severity feature
    // [Header("Severity of each answer (same size as Answers)")]
    // [SerializeField] Severity[] severities = new Severity[4];

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

    public int? GetCorrectAnswer()
    {
        if (int.TryParse(correctAnswerIndex, out int result))
        {
            return result;
        }

        return null;
    }

    public string GetFeedback(int index)
    {
        // Safety check
        if (feedback == null || index < 0 || index >= feedback.Length)
            return "";
        return feedback[index];
    }

    // TODO: Uncomment 'GetSeverity' function below to re-enable severity feature
    // public Severity GetSeverity(int index)
    // {
    //     // Default safe if data missing
    //     if (severities == null || index < 0 || index >= severities.Length)
    //         return Severity.None;
    //     return severities[index];
    // }
}