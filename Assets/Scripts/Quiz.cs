using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Quiz : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI questionText;
    [SerializeField] GameObject[] answerButtons;

    ScenarioManager scenario;

    void Start()
    {
        scenario = FindObjectOfType<ScenarioManager>();
    }

    public void ShowQuestion(QuestionSO question)
    {
        questionText.text = question.GetQuestion();

        for (int i = 0; i < answerButtons.Length; i++)
        {
            bool active = i < question.GetAnswerCount();
            answerButtons[i].SetActive(active);

            if (active)
            {
                Button btn = answerButtons[i].GetComponent<Button>();
                TextMeshProUGUI btnText = answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();

                btnText.text = question.GetAnswer(i);
                btn.onClick.RemoveAllListeners();

                int capturedIndex = i;
                btn.onClick.AddListener(() => scenario.OnAnswerSelected(capturedIndex));
            }
        }
    }
}