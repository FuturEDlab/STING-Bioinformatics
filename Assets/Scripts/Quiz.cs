using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Quiz : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI questionText;
    [SerializeField] GameObject[] answerButtons;

    /// <summary>
    /// Raised with the index of the answer the player clicked. Lets a scenario step subscribe
    /// without a hard manager dependency - see UIQuestionStep.
    /// </summary>
    public event Action<int> AnswerSelected;

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
            if (!active) return;

            Button btn = answerButtons[i].GetComponent<Button>();
            TextMeshProUGUI btnText = answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            btnText.text = question.GetAnswer(i);
            // if (btnText.text.Length < 1) return;
            
            btn.onClick.RemoveAllListeners();

            int capturedIndex = i;

            if (btnText.text.Length > 0)
            {
                btn.onClick.AddListener(() => RaiseAnswerSelected(capturedIndex));
            }

            // Ensure button is interactable when a new question shows
            btn.interactable = true;
        }
    }

    void RaiseAnswerSelected(int index)
    {
        AnswerSelected?.Invoke(index);

        // Legacy direct path, still used by ScenarioManager in SampleSceneV6.
        // Null in scenes driven by ScenarioController (e.g. Hospital Room).
        if (scenario != null)
            scenario.OnAnswerSelected(index);
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