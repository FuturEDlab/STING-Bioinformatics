using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Quiz : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI questionText;
    [SerializeField] GameObject[] answerButtons;

    // Raised (by answer index) whenever the player picks an answer. Lets the new
    // Scenario Controller's UIQuestionStep subscribe/unsubscribe without a hard
    // dependency on any particular manager.
    public event System.Action<int> AnswerSelected;

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
            // 'continue', not 'return': a 2-answer question must hide buttons 2 AND 3,
            // otherwise the leftovers from the previous question stay on screen.
            if (!active) continue;

            Button btn = answerButtons[i].GetComponent<Button>();
            TextMeshProUGUI btnText = answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            btnText.text = question.GetAnswer(i);
            // if (btnText.text.Length < 1) return;
            
            btn.onClick.RemoveAllListeners();

            int capturedIndex = i;

            if (btnText.text.Length > 0)
            {
                btn.onClick.AddListener(() =>
                {
                    AnswerSelected?.Invoke(capturedIndex);
                    if (scenario != null)
                        scenario.OnAnswerSelected(capturedIndex);
                });
            }

            // Ensure button is interactable when a new question shows
            btn.interactable = true;
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