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

    [Header("Question Audio")]
    [Tooltip("Button that plays the audio clip assigned to the currently displayed question.")]
    public Button questionAudioButton;
    [Tooltip("Audio source used for question narration. Assign a scene AudioSource here; this reference must be set on the panel instance, not in the QuestionBank asset.")]
    public AudioSource questionAudioSource;
    [Tooltip("Stateful button controller for the audio button. It receives the disabled state while feedback audio plays.")]
    public StatefulButtonSprites questionAudioButtonSprites;

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

    [Header("Testing")]
    [Tooltip("Open the panel on its question page the moment the scene starts, using the universal question. Answers 'can I see this thing in the headset at all?' without playing through to the quiz. The scenario reopens it normally when it gets there, so leaving this on only costs you a panel in your face at the start — but turn it off before shipping.")]
    public bool openQuestionOnStartForTesting;

    [Header("VR")]
    [Tooltip("Optional. Places the panel in front of the player when it opens, instead of leaving it at the world position it was authored at - which in a headset is usually several metres away and behind a wall. Left empty, one on this same object is used.")]
    public VRPanelAnchor vrAnchor;

    [Header("Scenario Integration")]
    [Tooltip("Optional. Raised when the player exits the panel, so a scenario step waiting on this channel can advance.")]
    public GameEvent panelClosedEvent;

    /// <summary>
    /// Raised when the player confirms an answer, with the answer index and whether it was
    /// correct. Lets a scenario step play its own per-answer narration without this class
    /// needing to know the scenario exists.
    /// </summary>
    public event System.Action<int, bool> AnswerConfirmed;

    [Header("Summary Page")]
    [Tooltip("Optional image slots on the summary page that show the selected colored answer sprites.")]
    public Image[] summaryAnswerImages;

    [Tooltip("Optional text field on the summary page showing how many answers were correct out of the total.")]
    public TextMeshProUGUI summaryScoreText;

    private QuestionData[] currentQuestionSequence;
    private int currentQuestionIndex;
    private int pendingAnswerIndex = -1;
    private Coroutine feedbackAudioCoroutine;
    private readonly System.Collections.Generic.List<Sprite> selectedAnswerColoredSprites = new System.Collections.Generic.List<Sprite>();
    private readonly System.Collections.Generic.List<bool> selectedAnswerCorrect = new System.Collections.Generic.List<bool>();

    private void Awake()
    {
        ResolveAnchor();
        ValidateContainer();
    }

    /// <summary>
    /// QP is the container switched on and off to show and hide the panel, so it has to be
    /// the object the PAGES hang off — never the object this component is on.
    ///
    /// Pointed at our own GameObject it is doubly wrong: ClosePanel would switch this very
    /// component off, and the pages' real parent is never switched on at all, so activating
    /// a page just activates it inside a dead parent and nothing renders. Both failures are
    /// invisible — no exception, no missing reference, just an empty panel — so it is worth
    /// correcting outright rather than leaving to be discovered in a headset.
    /// </summary>
    private void ValidateContainer()
    {
        if (QP == null || QP != gameObject)
            return;

        GameObject page = QPQuestion != null ? QPQuestion : QPTitle;

        if (page == null || page.transform.parent == null)
        {
            Debug.LogError($"{nameof(QuestionPanelManager)}: QP points at this same object, which cannot work — hiding the panel would switch this component off. Point it at the object holding the pages.", this);
            return;
        }

        QP = page.transform.parent.gameObject;
        Debug.LogWarning($"{nameof(QuestionPanelManager)}: QP pointed at '{name}', the object this component is on, so the pages' real parent was never switched on and the panel came up empty. Using '{QP.name}' instead. Fix the reference in the Inspector to silence this.", this);
    }

    /// <summary>
    /// Resolved in code rather than left to the Inspector: the anchor belongs on the same
    /// canvas root this lives on, so requiring somebody to remember to drag it in is a step
    /// that can only be got wrong. Called again from every entry point because Awake is not
    /// guaranteed to have run - anything that switches this object off during its own Awake
    /// defers ours indefinitely, and then the panel opens wherever it was left in the scene
    /// instead of in front of the player.
    /// </summary>
    private void ResolveAnchor()
    {
        if (vrAnchor == null)
            vrAnchor = GetComponent<VRPanelAnchor>();
    }

    void Start()
    {
        if (majorSelectionGroup != null)
        {
            majorSelectionGroup.OnSelectionChanged += HandleMajorSelectionChanged;
        }

        UpdateMajorContinueState();

        if (openQuestionOnStartForTesting)
            ShowUniversalQuestionForTesting();
    }

    /// <summary>
    /// Opens the panel straight away with the shared universal question. Exists for headset
    /// testing, where the console is not readable and the only way to tell a panel that is
    /// mispositioned from one that is never shown at all is to put it up unconditionally.
    /// </summary>
    [ContextMenu("Show Universal Question (testing)")]
    public void ShowUniversalQuestionForTesting()
    {
        if (questionBank == null)
        {
            Debug.LogError($"{nameof(QuestionPanelManager)}: no Question Bank assigned, so there is nothing to show. If BioQuestions IS assigned in the Inspector, then its script reference is broken — check the asset reads 'Script: QuestionBank'.", this);
            return;
        }

        ShowSingleQuestion(questionBank.universalQuestion);
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

        StopAllQuestionAudio();

        // Showing a question means it is answerable. Without this the buttons stay locked
        // from whatever disabled them last: confirming an answer switches them off, so the
        // in-simulation quiz used to leave the panel dead for the Scene 4 assessment.
        SetAnswerButtonsInteractable(true);

        DeselectAllAnswers();
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

        UpdateQuestionAudioButton(question);

        CreateAnswerButtons(question);

        if (layoutUpdater != null)
        {
            layoutUpdater.UpdateLayout();
        }
    }

    private void UpdateQuestionAudioButton(QuestionData question)
    {
        if (questionAudioButton == null)
            return;

        bool canPlay = question != null && question.questionAudioClip != null && questionAudioSource != null;
        questionAudioButton.interactable = canPlay;
        if (questionAudioButtonSprites != null)
            questionAudioButtonSprites.SetDisabled(!canPlay);
    }

    public void PlayQuestionAudio()
    {
        if (questionAudioSource == null || currentQuestionSequence == null || currentQuestionIndex < 0 || currentQuestionIndex >= currentQuestionSequence.Length)
            return;

        var question = currentQuestionSequence[currentQuestionIndex];
        if (question == null || question.questionAudioClip == null)
            return;

        StopAllQuestionAudio();
        questionAudioSource.clip = question.questionAudioClip;
        questionAudioSource.Play();
    }

    private void StopAllQuestionAudio()
    {
        if (feedbackAudioCoroutine != null)
        {
            StopCoroutine(feedbackAudioCoroutine);
            feedbackAudioCoroutine = null;
        }

        if (questionAudioSource != null)
            questionAudioSource.Stop();

        if (currentQuestionSequence != null && currentQuestionIndex >= 0 && currentQuestionIndex < currentQuestionSequence.Length)
            UpdateQuestionAudioButton(currentQuestionSequence[currentQuestionIndex]);
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
        {
            AdvanceToNextQuestion();
            return;
        }

        int confirmedIndex = pendingAnswerIndex;

        StoreSelectedAnswerSprite(pendingAnswerIndex);
        RecordAnswerSelection(pendingAnswerIndex);
        ShowAnswerExplanation(pendingAnswerIndex);
        PlayExplanationAudio(pendingAnswerIndex);
        pendingAnswerIndex = -1;

        SetAnswerButtonsInteractable(false);
        SetQuestionContinueButtonInteractable(true);

        AnswerConfirmed?.Invoke(confirmedIndex, IsAnswerCorrect(confirmedIndex));
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

        selectedAnswerColoredSprites.Clear();
        selectedAnswerCorrect.Clear();

        currentQuestionSequence = new[] { question };
        currentQuestionIndex = 0;

        // Switching QP on achieves nothing if this object — or anything above it — is
        // inactive, which it usually is, because the panel is kept hidden during the
        // simulation. Walk up and switch the whole chain on first.
        EnsureHierarchyActive();
        ResolveAnchor();
        ValidateContainer();

        // Placed before the pages are switched on, so the panel never appears for a frame at
        // wherever it was left last time.
        if (vrAnchor != null) vrAnchor.Place();

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

        ReportVisibility("ShowSingleQuestion");
    }

    /// <summary>
    /// Says in one console line why the panel is or is not on screen. "The question never
    /// appeared" has too many possible causes to guess at from inside a headset — an
    /// unassigned page reference, a parent left switched off, a canvas sitting behind the
    /// player — and every one of them looks identical while you are wearing it.
    /// </summary>
    private void ReportVisibility(string calledFrom)
    {
        var report = new System.Text.StringBuilder();
        report.Append($"[QuestionPanelManager] {calledFrom} on '{name}': ");

        if (QP == null)
            report.Append("PROBLEM the QP field is empty, so the panel container is never switched on or off — showing and hiding the panel does nothing. Assign the object holding the pages. ");
        else
            report.Append($"container '{QP.name}' {(QP.activeInHierarchy ? "active" : "INACTIVE, so nothing on it can be seen")}. ");

        if (QPQuestion != null)
            report.Append($"question page {(QPQuestion.activeInHierarchy ? "active" : "INACTIVE")}. ");

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            report.Append($"canvas {canvas.renderMode} at {canvas.transform.position}, world scale {canvas.transform.lossyScale.x:0.0000}. ");

            Camera head = Camera.main;

            if (head == null)
            {
                report.Append("NO enabled camera tagged MainCamera, so nothing could have placed it in front of the player.");
            }
            else
            {
                Vector3 toPanel = canvas.transform.position - head.transform.position;
                float ahead = Vector3.Dot(head.transform.forward, toPanel.normalized);

                report.Append($"{toPanel.magnitude:0.0} m from the camera, {(ahead > 0.5f ? "in front of" : ahead > 0f ? "off to the side of" : "BEHIND")} it.");
            }
        }

        Debug.Log(report.ToString(), this);
    }

    /// <summary>Hide the whole panel without running the exit page. Pairs with <see cref="ShowSingleQuestion"/>.</summary>
    public void ClosePanel()
    {
        StopAllQuestionAudio();

        if (QPQuestion != null) QPQuestion.SetActive(false);
        if (QP != null) QP.SetActive(false);

        if (vrAnchor != null) vrAnchor.Release();

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
        ResolveAnchor();
        ValidateContainer();

        if (vrAnchor != null) vrAnchor.Place();

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

    private void PlayExplanationAudio(int answerIndex)
    {
        if (questionAudioSource == null || currentQuestionSequence == null || currentQuestionIndex < 0 || currentQuestionIndex >= currentQuestionSequence.Length)
            return;

        var question = currentQuestionSequence[currentQuestionIndex];
        if (question == null || question.answers == null || answerIndex < 0 || answerIndex >= question.answers.Length)
            return;

        var explanationAudio = question.answers[answerIndex].isCorrect
            ? question.explanationAudioCorrect
            : question.explanationAudioIncorrect;

        StopAllQuestionAudio();

        if (explanationAudio == null)
            return;

        questionAudioSource.clip = explanationAudio;
        questionAudioSource.Play();
        SetQuestionAudioButtonDisabled(true);
        feedbackAudioCoroutine = StartCoroutine(EnableQuestionAudioAfterFeedback(explanationAudio.length));
    }

    private IEnumerator EnableQuestionAudioAfterFeedback(float duration)
    {
        yield return new WaitForSeconds(duration);

        feedbackAudioCoroutine = null;
        if (currentQuestionSequence == null || currentQuestionIndex < 0 || currentQuestionIndex >= currentQuestionSequence.Length)
            yield break;

        UpdateQuestionAudioButton(currentQuestionSequence[currentQuestionIndex]);
    }

    private void SetQuestionAudioButtonDisabled(bool disabled)
    {
        if (questionAudioButton != null)
            questionAudioButton.interactable = !disabled;

        if (questionAudioButtonSprites != null)
            questionAudioButtonSprites.SetDisabled(disabled);
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
