using UnityEngine;
using System;

[System.Serializable]
public class AnswerButtonStateSprites
{
    public Sprite defaultSprite;
    public Sprite hoverSprite;
    public Sprite selectedSprite;
    public Sprite selectedHoverSprite;
}

[System.Serializable]
public class AnswerData
{
    public string answerText;
    public Sprite answerImage;
    public bool isCorrect;
    public AnswerButtonStateSprites buttonStateSprites;
}

[System.Serializable]
public class QuestionData
{
    public string questionText;
    public float questionTextFieldHeight;
    public Sprite[] ColoredAnswerSprites;
    public Sprite explanationImageCorrect;
    public Sprite explanationImageIncorrect;
    public AnswerData[] answers;
}

[System.Serializable]
public class MajorQuestionSet
{
    public string majorName;
    public QuestionData majorQuestion;
}

[CreateAssetMenu(menuName = "Quiz/Question Bank")]
public class QuestionBank : ScriptableObject
{
    public QuestionData universalQuestion;
    public MajorQuestionSet[] majorQuestionSets;
}