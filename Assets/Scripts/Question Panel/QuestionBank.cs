using UnityEngine;

/// <summary>
/// The authored question set: the one question everybody is asked, plus a follow-up per
/// major. Shared by the in-simulation quiz and the end-of-experience assessment so the
/// wording and answer artwork live in exactly one place.
///
/// This lives in a file of its own on purpose. Unity only builds a MonoScript for the type
/// whose name matches the file it is in, and a ScriptableObject asset needs that MonoScript
/// to know what it is. Declared alongside the plain [Serializable] classes in
/// QuestionDataDefinitions.cs, as it was, BioQuestions.asset had no script to point at: it
/// loaded as an untyped object, every typed reference to it came back null, and both the
/// quiz step and the assessment silently found no questions.
/// </summary>
[CreateAssetMenu(menuName = "Quiz/Question Bank")]
public class QuestionBank : ScriptableObject
{
    public QuestionData universalQuestion;
    public MajorQuestionSet[] majorQuestionSets;
}
