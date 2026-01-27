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

                // Ensure button is interactable when a new question shows
                btn.interactable = true;
            }
        }
    }

    // New helper so other scripts can enable/disable answer buttons
    public void SetButtonsInteractable(bool interactable)
    {
        foreach (var btnGo in answerButtons)
        {
            if (btnGo == null) continue;
            Button btn = btnGo.GetComponent<Button>();
            if (btn != null)
                btn.interactable = interactable;
        }
    }
}