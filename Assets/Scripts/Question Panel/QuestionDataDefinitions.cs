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

// QuestionBank used to live here. It is a ScriptableObject, and Unity only builds the
// MonoScript such an asset needs for the type whose name matches its file - so it now has
// a file of its own, QuestionBank.cs. The plain [Serializable] classes above have no such
// requirement and are fine sharing this one.
