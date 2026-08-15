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

    [Tooltip("Skip the universal question in the full assessment. The script asks it twice — once in the simulation after the Methotrexate alert, and again as Question 1 here. Tick this to ask it only during the simulation and go straight to the major-specific question.")]
    public bool skipUniversalQuestionInAssessment = false;

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

    [Header("Scenario Integration")]
    [Tooltip("Optional. Raised when the player exits the panel, so a scenario step waiting on this channel can advance.")]
    public GameEvent panelClosedEvent;

    /// <summary>
    /// Raised when the player confirms an answer, with the answer index and whether it was
    /// correct. Lets a scenario step play its own per-answer narration without this class
    /// needing to know the scenario exists.
    /// </summary>
    public event System.Action<int, bool> AnswerConfirmed;

    // Set while the scenario is driving one question through this panel. In that mode the
    // panel does not run its own 10-second countdown or fall through to the summary page —
    // the scenario decides when to move on, once its narration has finished.
    private bool singleQuestionMode;

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
        RestoreHierarchyActive();

        if (panelClosedEvent != null)
            panelClosedEvent.Raise();
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
            else if (skipUniversalQuestionInAssessment)
            {
                currentQuestionSequence = new[] { majorSet.majorQuestion };
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

        // Showing a question means it is answerable. Without this the buttons stay locked
        // from whatever disabled them last: confirming an answer switches them off, so the
        // in-simulation quiz used to leave the panel dead for the Scene 4 assessment.
        SetAnswerButtonsInteractable(true);

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

        int confirmedIndex = pendingAnswerIndex;

        StoreSelectedAnswerSprite(pendingAnswerIndex);
        RecordAnswerSelection(pendingAnswerIndex);
        ShowAnswerExplanation(pendingAnswerIndex);
        pendingAnswerIndex = -1;
        UpdateQuestionContinueState();

        if (answerCountdownCoroutine != null)
        {
            StopCoroutine(answerCountdownCoroutine);
            answerCountdownCoroutine = null;
        }

        SetAnswerButtonsInteractable(false);
        SetQuestionContinueButtonInteractable(false);

        AnswerConfirmed?.Invoke(confirmedIndex, IsAnswerCorrect(confirmedIndex));

        // The scenario runs the pacing in single-question mode: its narrator line plays
        // over the explanation image, and it closes the panel when the line ends.
        if (!singleQuestionMode)
            answerCountdownCoroutine = StartCoroutine(RunAnswerCountdown());
    }

    private bool IsAnswerCorrect(int answerIndex)
    {
        if (currentQuestionSequence == null || currentQuestionIndex < 0 || currentQuestionIndex >= currentQuestionSequence.Length)
            return false;

        var question = currentQuestionSequence[currentQuestionIndex];
        if (question?.answers == null || answerIndex < 0 || answerIndex >= question.answers.Length)
            return false;

        return question.answers[answerIndex].isCorrect;
    }

    /// <summary>
    /// Show this panel for exactly one question, skipping the title/major pages and the
    /// summary. Used by the scenario for the in-simulation quiz, so the same panel and the
    /// same authored question data serve both that and the post-experience assessment.
    /// </summary>
    public void ShowSingleQuestion(QuestionData question)
    {
        if (question == null)
        {
            Debug.LogWarning($"{nameof(QuestionPanelManager)}: ShowSingleQuestion was given no question.");
            return;
        }

        singleQuestionMode = true;
        selectedAnswerColoredSprites.Clear();
        selectedAnswerCorrect.Clear();

        currentQuestionSequence = new[] { question };
        currentQuestionIndex = 0;

        // Switching QP on achieves nothing if this object — or anything above it — is
        // inactive, which it usually is, because the panel is kept hidden during the
        // simulation. Walk up and switch the whole chain on first.
        EnsureHierarchyActive();

        if (QP != null) QP.SetActive(true);
        if (QPTitle != null) QPTitle.SetActive(false);
        if (QPMajor != null) QPMajor.SetActive(false);
        if (QPSummary != null) QPSummary.SetActive(false);
        if (QPExit != null) QPExit.SetActive(false);

        if (QPQuestion != null)
            QPQuestion.SetActive(true);
        else
            Debug.LogWarning($"{nameof(QuestionPanelManager)}: QPQuestion is not assigned, so the question page cannot be shown.", this);

        SetAnswerButtonsInteractable(true);
        ShowCurrentQuestion();
    }

    /// <summary>Hide the whole panel without running the exit page. Pairs with <see cref="ShowSingleQuestion"/>.</summary>
    public void ClosePanel()
    {
        singleQuestionMode = false;

        if (answerCountdownCoroutine != null)
        {
            StopCoroutine(answerCountdownCoroutine);
            answerCountdownCoroutine = null;
        }

        if (QPQuestion != null) QPQuestion.SetActive(false);
        if (QP != null) QP.SetActive(false);

        // Hand the panel back in a usable state — confirming an answer locked the buttons,
        // and the next thing to open it should not inherit that.
        SetAnswerButtonsInteractable(true);
        pendingAnswerIndex = -1;
        UpdateQuestionContinueState();

        // Put back whatever we switched on to show the panel, so the room is not left with
        // an invisible-but-active canvas swallowing pointer input.
        RestoreHierarchyActive();
    }

    private readonly System.Collections.Generic.List<GameObject> activatedByUs = new System.Collections.Generic.List<GameObject>();

    /// <summary>
    /// Switch this object and every inactive ancestor on, remembering what we changed.
    /// Needed because SetActive on a child does nothing while a parent is inactive — the
    /// panel would silently never appear.
    /// </summary>
    private void EnsureHierarchyActive()
    {
        activatedByUs.Clear();

        for (Transform t = transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf)
            {
                activatedByUs.Add(t.gameObject);
                t.gameObject.SetActive(true);
            }
        }

        // Activate outermost-first so children are enabled into an already-live parent.
        activatedByUs.Reverse();
    }

    private void RestoreHierarchyActive()
    {
        for (int i = activatedByUs.Count - 1; i >= 0; i--)
        {
            if (activatedByUs[i] != null)
                activatedByUs[i].SetActive(false);
        }

        activatedByUs.Clear();
    }

    /// <summary>
    /// Open the panel on its first page. Wire a SceneEventRelay for EV_OpenAssessment to
    /// this for Scene 4 — it handles the inactive-parent problem the same way.
    /// </summary>
    public void OpenPanel()
    {
        EnsureHierarchyActive();

        if (QP != null) QP.SetActive(true);
        if (QPTitle != null) QPTitle.SetActive(true);
        if (QPMajor != null) QPMajor.SetActive(false);
        if (QPQuestion != null) QPQuestion.SetActive(false);
        if (QPSummary != null) QPSummary.SetActive(false);
        if (QPExit != null) QPExit.SetActive(false);
    }

    /// <summary>Re-enable the buttons so the same question can be asked again after a wrong answer.</summary>
    public void ReaskCurrentQuestion()
    {
        SetAnswerButtonsInteractable(true);
        ShowCurrentQuestion();
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
