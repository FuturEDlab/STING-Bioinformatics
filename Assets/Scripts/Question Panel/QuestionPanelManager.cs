using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionPanelManager : MonoBehaviour
{
    // QP = Question Panel
    // Question Panel GameObjects
    [Header("Question Panel Page GameObjects")]
    public GameObject QP;
    public GameObject QPTitle;
    public GameObject QPMajor;
    public GameObject QPQuestion;
    public GameObject QPSummary;
    public GameObject QPExit;

    [Header("Major Selection")]
    public ButtonSelectionGroup majorSelectionGroup;
    public Button majorSelectButton;
    public StatefulButtonSprites majorSelectButtonSprites;

    [Header("Question Bank")]
    public QuestionBank questionBank;

    [Header("Question Page UI")]
    public TextMeshProUGUI questionTMP;
    public Image explanationImage;

    [Header("Question Continue")]
    public Button questionContinueButton;
    public StatefulButtonSprites questionContinueButtonSprites;

    [Header("Answer Button Setup")]
    [Tooltip("Existing buttons on the question panel prefab.")]
    public Button[] answerButtons;

    [Tooltip("Optional text field for each existing answer button. If left empty, the manager looks for a TextMeshProUGUI child.")]
    public TextMeshProUGUI[] answerButtonTexts;

    [Tooltip("Optional ButtonSelectionGroup used when the existing answer buttons share one group.")]
    public ButtonSelectionGroup answerSelectionGroup;

    [Tooltip("Parent transform where answer buttons are instantiated when not using existing buttons.")]
    public Transform answerButtonContainer;

    [Tooltip("Prefab used to create each answer button when not using existing buttons.")]
    public GameObject answerButtonPrefab;

    [Tooltip("Optional helper to recalculate layout after populating content.")]
    public UpdateAnswerListHeight layoutUpdater;

    [Header("Summary Page")]
    [Tooltip("Optional image slots on the summary page that show the selected colored answer sprites.")]
    public Image[] summaryAnswerImages;

    [Tooltip("Optional text field on the summary page showing how many answers were correct out of the total.")]
    public TextMeshProUGUI summaryScoreText;

    private QuestionData[] currentQuestionSequence;
    private int currentQuestionIndex;
    private int pendingAnswerIndex = -1;
    private Coroutine answerCountdownCoroutine;
    private TextMeshProUGUI questionContinueButtonText;
    private string questionContinueButtonOriginalText = "Continue";
    private readonly System.Collections.Generic.List<Sprite> selectedAnswerColoredSprites = new System.Collections.Generic.List<Sprite>();
    private readonly System.Collections.Generic.List<bool> selectedAnswerCorrect = new System.Collections.Generic.List<bool>();

    void Start()
    {
        if (majorSelectionGroup != null)
        {
            majorSelectionGroup.OnSelectionChanged += HandleMajorSelectionChanged;
        }

        if (questionContinueButton != null)
        {
            questionContinueButtonText = questionContinueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (questionContinueButtonText != null)
            {
                questionContinueButtonOriginalText = questionContinueButtonText.text;
            }
        }

        UpdateMajorContinueState();
    }

    private void OnDestroy()
    {
        if (majorSelectionGroup != null)
        {
            majorSelectionGroup.OnSelectionChanged -= HandleMajorSelectionChanged;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void GoToMajor()
    {
        Debug.Log("Select your major...");
        QPTitle.SetActive(false);
        QPMajor.SetActive(true);
        UpdateMajorContinueState();
    }

    public void GoToQuestion()
    {
        if (majorSelectionGroup != null && !majorSelectionGroup.HasSelection)
        {
            Debug.LogWarning("Cannot continue to question page until a major is selected.");
            return;
        }

        PrepareQuestionSequence(majorSelectionGroup.SelectedIndex);

        Debug.Log("Answer the question(s)...");
        QPMajor.SetActive(false);
        QPQuestion.SetActive(true);
        ShowCurrentQuestion();
    }

    public void GoToSummary()
    {
        Debug.Log("Showing summary of selections...");
        QPQuestion.SetActive(false);
        //ShowSummarySelections();
        QPSummary.SetActive(true);
    }

    public void GoToExit()
    {
        Debug.Log("Showing Exit Page...");
        QPSummary.SetActive(false);
        QPExit.SetActive(true);
    }

    public void ExitQuestionPanel()
    {
        Debug.Log("Exiting Question Panel...");
        QPExit.SetActive(false);
        QP.SetActive(false);
    }

    public void HandleMajorSelectionChanged(int selectedIndex)
    {
        Debug.Log($"QuestionPanelManager: major selected index {selectedIndex}");
        PrepareQuestionSequence(selectedIndex);
        UpdateMajorContinueState();
    }

    private void PrepareQuestionSequence(int majorIndex)
    {
        selectedAnswerColoredSprites.Clear();
        selectedAnswerCorrect.Clear();

        if (questionBank == null)
        {
            Debug.LogWarning($"{nameof(QuestionPanelManager)}: questionBank is not assigned.");
            currentQuestionSequence = new QuestionData[0];
            return;
        }

        if (questionBank.universalQuestion == null)
        {
            Debug.LogWarning($"{nameof(QuestionPanelManager)}: universalQuestion is missing in the question bank.");
            currentQuestionSequence = new QuestionData[0];
            return;
        }

        if (majorIndex < 0 || questionBank.majorQuestionSets == null || majorIndex >= questionBank.majorQuestionSets.Length)
        {
            Debug.LogWarning($"{nameof(QuestionPanelManager)}: majorIndex {majorIndex} is invalid. Showing only the universal question.");
            currentQuestionSequence = new[] { questionBank.universalQuestion };
        }
        else
        {
            var majorSet = questionBank.majorQuestionSets[majorIndex];
            if (majorSet == null || majorSet.majorQuestion == null)
            {
                Debug.LogWarning($"{nameof(QuestionPanelManager)}: major-specific question data is missing for majorIndex {majorIndex}.");
                currentQuestionSequence = new[] { questionBank.universalQuestion };
            }
            else
            {
                currentQuestionSequence = new[] { questionBank.universalQuestion, majorSet.majorQuestion };
            }
        }

        currentQuestionIndex = 0;
    }

    private void ShowCurrentQuestion()
    {
        if (currentQuestionSequence == null || currentQuestionSequence.Length == 0)
            return;

        if (currentQuestionIndex < 0 || currentQuestionIndex >= currentQuestionSequence.Length)
            return;

        DeselectAllAnswers();
        ResetQuestionContinueButtonText();
        PopulateQuestionUI(currentQuestionSequence[currentQuestionIndex]);
    }

    private void PopulateQuestionUI(QuestionData question)
    {
        if (questionTMP != null)
            questionTMP.text = question.questionText ?? string.Empty;

        if (layoutUpdater != null)
        {
            layoutUpdater.SetQuestionTextFieldHeight(question.questionTextFieldHeight);
            Debug.Log($"Question text field height applied: {question.questionTextFieldHeight}");
        }

        if (explanationImage != null)
        {
            explanationImage.sprite = null;
            explanationImage.gameObject.SetActive(false);
        }

        CreateAnswerButtons(question);

        if (layoutUpdater != null)
        {
            layoutUpdater.UpdateLayout();
        }
    }

    private void CreateAnswerButtons(QuestionData question)
    {
        if (question == null || question.answers == null || question.answers.Length == 0)
            return;

        if (answerButtons != null && answerButtons.Length > 0)
        {
            PopulateExistingAnswerButtons(question);
            return;
        }

        PopulateDynamicAnswerButtons(question);
    }

    private void PopulateExistingAnswerButtons(QuestionData question)
    {
        var answers = question.answers;
        var selectionGroups = GetAnswerSelectionGroups();
        foreach (var group in selectionGroups)
        {
            group.ClearSelection();
        }

        pendingAnswerIndex = -1;
        if (explanationImage != null)
            explanationImage.gameObject.SetActive(false);
        UpdateQuestionContinueState();

        for (int i = 0; i < answerButtons.Length; i++)
        {
            var button = answerButtons[i];
            bool shouldShow = answers != null && i < answers.Length;
            button.gameObject.SetActive(shouldShow);

            if (!shouldShow)
                continue;

            var answer = answers[i];
            // Do not apply text to answer buttons during question answering.
            // (Preserve answerButtonTexts field for future use.)

            var spriteToUse = GetColoredAnswerSprite(question, i);
            SetAnswerButtonImage(button.gameObject, spriteToUse);
            SetAnswerButtonStateSprites(button.gameObject, answer.buttonStateSprites);

            button.onClick.RemoveAllListeners();
            int capturedIndex = i;
            button.onClick.AddListener(() => OnAnswerPending(capturedIndex, selectionGroups));
        }
    }

    private void PopulateDynamicAnswerButtons(QuestionData question)
    {
        if (answerButtonContainer == null || answerButtonPrefab == null)
            return;

        var answers = question.answers;
        if (answers == null || answers.Length == 0)
            return;

        for (int i = answerButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(answerButtonContainer.GetChild(i).gameObject);
        }

        pendingAnswerIndex = -1;
        if (explanationImage != null)
            explanationImage.gameObject.SetActive(false);
        UpdateQuestionContinueState();

        for (int i = 0; i < answers.Length; i++)
        {
            var answer = answers[i];
            var buttonGO = Instantiate(answerButtonPrefab, answerButtonContainer);

            // Do not apply text to answer buttons during question answering.
            // (Preserve prefab text child if present.)

            var spriteToUse = GetColoredAnswerSprite(question, i);
            SetAnswerButtonImage(buttonGO, spriteToUse);
            SetAnswerButtonStateSprites(buttonGO, answer.buttonStateSprites);

            var buttonComponent = buttonGO.GetComponent<Button>();
            if (buttonComponent != null)
            {
                int capturedIndex = i;
                buttonComponent.onClick.RemoveAllListeners();
                buttonComponent.onClick.AddListener(() => OnAnswerPending(capturedIndex, GetAnswerSelectionGroups()));
            }
        }
    }

    private ButtonSelectionGroup[] GetAnswerSelectionGroups()
    {
        if (answerSelectionGroup != null)
            return new[] { answerSelectionGroup };

        if (answerButtons == null || answerButtons.Length == 0)
            return new ButtonSelectionGroup[0];

        var groups = new System.Collections.Generic.List<ButtonSelectionGroup>(answerButtons.Length);
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null)
                continue;

            var group = answerButtons[i].GetComponent<ButtonSelectionGroup>();
            if (group != null)
                groups.Add(group);
        }

        return groups.ToArray();
    }

    private void OnAnswerPending(int answerIndex, ButtonSelectionGroup[] selectionGroups)
    {
        pendingAnswerIndex = answerIndex;

        foreach (var group in selectionGroups)
        {
            group.SelectButton(answerIndex);
        }

        UpdateQuestionContinueState();
    }

    public void ConfirmAnswer()
    {
        if (pendingAnswerIndex < 0)
            return;

        StoreSelectedAnswerSprite(pendingAnswerIndex);
        RecordAnswerSelection(pendingAnswerIndex);
        ShowAnswerExplanation(pendingAnswerIndex);
        pendingAnswerIndex = -1;
        UpdateQuestionContinueState();

        if (answerCountdownCoroutine != null)
        {
            StopCoroutine(answerCountdownCoroutine);
        }

        SetAnswerButtonsInteractable(false);
        SetQuestionContinueButtonInteractable(false);
        answerCountdownCoroutine = StartCoroutine(RunAnswerCountdown());
    }

    private void ShowAnswerExplanation(int answerIndex)
    {
        if (explanationImage == null || currentQuestionSequence == null || currentQuestionSequence.Length == 0)
            return;

        var question = currentQuestionSequence[currentQuestionIndex];
        if (question == null)
            return;

        var answer = question.answers != null && answerIndex >= 0 && answerIndex < question.answers.Length
            ? question.answers[answerIndex]
            : null;

        if (answer == null)
            return;

        var sprite = answer.isCorrect ? question.explanationImageCorrect : question.explanationImageIncorrect;
        explanationImage.sprite = sprite;
        explanationImage.gameObject.SetActive(sprite != null);
    }

    private void RecordAnswerSelection(int answerIndex)
    {
        if (currentQuestionSequence == null || currentQuestionSequence.Length == 0)
            return;

        var question = currentQuestionSequence[currentQuestionIndex];
        if (question == null || question.answers == null || answerIndex < 0 || answerIndex >= question.answers.Length)
            return;

        var selectedAnswer = question.answers[answerIndex];
        Debug.Log($"Answer selected: {selectedAnswer.answerText} (correct: {selectedAnswer.isCorrect})");
    }

    private void SetAnswerButtonImage(GameObject buttonGO, Sprite sprite)
    {
        if (buttonGO == null)
            return;

        var image = buttonGO.GetComponentInChildren<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            image.gameObject.SetActive(sprite != null);
        }
    }

    private void SetAnswerButtonStateSprites(GameObject buttonGO, AnswerButtonStateSprites stateSprites)
    {
        if (buttonGO == null || stateSprites == null)
            return;

        var state = buttonGO.GetComponentInChildren<StatefulButtonSprites>();
        if (state == null)
            return;

        state.SetStateSprites(
            stateSprites.defaultSprite,
            stateSprites.hoverSprite,
            stateSprites.selectedSprite,
            stateSprites.selectedHoverSprite,
            null);
    }

    private void SetAnswerButtonsInteractable(bool enabled)
    {
        if (answerButtons != null && answerButtons.Length > 0)
        {
            foreach (var button in answerButtons)
            {
                if (button == null)
                    continue;

                button.interactable = enabled;
                var state = button.GetComponentInChildren<StatefulButtonSprites>();
                if (state != null)
                {
                    state.SetDisabled(!enabled);
                }
            }

            return;
        }

        if (answerButtonContainer == null)
            return;

        for (int i = 0; i < answerButtonContainer.childCount; i++)
        {
            var child = answerButtonContainer.GetChild(i).gameObject;
            if (child == null)
                continue;

            var button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = enabled;
            }

            var state = child.GetComponentInChildren<StatefulButtonSprites>();
            if (state != null)
            {
                state.SetDisabled(!enabled);
            }
        }
    }

    private void SetQuestionContinueButtonInteractable(bool enabled)
    {
        if (questionContinueButton != null)
        {
            questionContinueButton.interactable = enabled;
        }

        if (questionContinueButtonSprites != null)
        {
            questionContinueButtonSprites.SetDisabled(!enabled);
        }
    }

    private void ResetQuestionContinueButtonText()
    {
        if (questionContinueButtonText != null)
        {
            questionContinueButtonText.text = questionContinueButtonOriginalText;
        }
    }

    private void SetQuestionContinueButtonText(string text)
    {
        if (questionContinueButtonText != null)
        {
            questionContinueButtonText.text = text;
        }
    }

    private void DeselectAllAnswers()
    {
        pendingAnswerIndex = -1;
        UpdateQuestionContinueState();

        var selectionGroups = GetAnswerSelectionGroups();
        foreach (var group in selectionGroups)
        {
            group.ClearSelection();
        }

        if (answerButtons != null && answerButtons.Length > 0)
        {
            foreach (var button in answerButtons)
            {
                if (button == null)
                    continue;

                var state = button.GetComponentInChildren<StatefulButtonSprites>();
                if (state != null)
                {
                    state.SetSelected(false);
                }
            }
        }
        else if (answerButtonContainer != null)
        {
            for (int i = 0; i < answerButtonContainer.childCount; i++)
            {
                var child = answerButtonContainer.GetChild(i).gameObject;
                if (child == null)
                    continue;

                var state = child.GetComponentInChildren<StatefulButtonSprites>();
                if (state != null)
                {
                    state.SetSelected(false);
                }
            }
        }
    }

    private IEnumerator RunAnswerCountdown()
    {
        const int countdownSeconds = 10;

        for (int remaining = countdownSeconds; remaining > 0; remaining--)
        {
            SetQuestionContinueButtonText($"Next in {remaining}s");
            yield return new WaitForSeconds(1f);
        }

        ResetQuestionContinueButtonText();
        answerCountdownCoroutine = null;
        AdvanceToNextQuestion();
    }

    private void AdvanceToNextQuestion()
    {
        if (currentQuestionSequence == null || currentQuestionSequence.Length == 0)
            return;

        currentQuestionIndex++;
        if (currentQuestionIndex < currentQuestionSequence.Length)
        {
            ShowCurrentQuestion();
            if (layoutUpdater != null)
            {
                layoutUpdater.UpdateLayout();
            }
            SetAnswerButtonsInteractable(true);
        }
        else
        {
            Debug.Log("All questions complete.");
            QPQuestion.SetActive(false);
            ShowSummarySelections();
            if (QPSummary != null)
            {
                QPSummary.SetActive(true);
            }
        }
    }

    public void UpdateMajorContinueState()
    {
        bool enabled = majorSelectionGroup != null && majorSelectionGroup.HasSelection;

        if (majorSelectButton != null)
        {
            majorSelectButton.interactable = enabled;
        }

        if (majorSelectButtonSprites != null)
        {
            majorSelectButtonSprites.SetDisabled(!enabled);
        }
    }

    private void UpdateQuestionContinueState()
    {
        bool enabled = pendingAnswerIndex >= 0;

        if (questionContinueButton != null)
        {
            questionContinueButton.interactable = enabled;
        }

        if (questionContinueButtonSprites != null)
        {
            questionContinueButtonSprites.SetDisabled(!enabled);
        }
    }

    private Sprite GetColoredAnswerSprite(QuestionData question, int answerIndex)
    {
        if (question == null || question.ColoredAnswerSprites == null || answerIndex < 0 || answerIndex >= question.ColoredAnswerSprites.Length)
            return null;

        return question.ColoredAnswerSprites[answerIndex];
    }

    private void StoreSelectedAnswerSprite(int answerIndex)
    {
        if (currentQuestionSequence == null || currentQuestionSequence.Length == 0)
            return;

        var question = currentQuestionSequence[currentQuestionIndex];
        if (question == null || question.answers == null || answerIndex < 0 || answerIndex >= question.answers.Length)
            return;

        selectedAnswerColoredSprites.Add(GetColoredAnswerSprite(question, answerIndex));
        selectedAnswerCorrect.Add(question.answers[answerIndex].isCorrect);
    }

    private void ShowSummarySelections()
    {
        if (summaryAnswerImages != null)
        {
            for (int i = 0; i < summaryAnswerImages.Length; i++)
            {
                var image = summaryAnswerImages[i];
                if (image == null)
                    continue;

                if (i < selectedAnswerColoredSprites.Count && selectedAnswerColoredSprites[i] != null)
                {
                    image.sprite = selectedAnswerColoredSprites[i];
                    image.gameObject.SetActive(true);
                }
                else
                {
                    image.sprite = null;
                    image.gameObject.SetActive(false);
                }
            }
        }

        UpdateSummaryScoreText();
    }

    private void UpdateSummaryScoreText()
    {
        if (summaryScoreText == null)
            return;

        int correctCount = 0;
        foreach (var isCorrect in selectedAnswerCorrect)
        {
            if (isCorrect)
                correctCount++;
        }

        int totalQuestions = currentQuestionSequence != null ? currentQuestionSequence.Length : selectedAnswerCorrect.Count;
        summaryScoreText.text = $"{correctCount} / {totalQuestions}";
    }
}
