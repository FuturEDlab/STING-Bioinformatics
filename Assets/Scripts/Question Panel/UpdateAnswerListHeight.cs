using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Updates LayoutElement preferred heights using TextMeshProUGUI measurements only.
// This avoids ContentSizeFitter usage on children of a LayoutGroup.
public class UpdateAnswerListHeight : MonoBehaviour
{
    [Header("Question (TMP)")]
    public TextMeshProUGUI questionTMP;
    public LayoutElement questionLayoutElement;

    [Header("Explanation")]
    public Image explanationImage;
    public LayoutElement explanationLayoutElement;

    [Header("Answers")]
    public RectTransform answersContainer; // parent with VerticalLayoutGroup
    public bool updateAnswerButtons = true;
    public float minAnswerButtonHeight = 40f;

    [Header("Layout")]
    public RectTransform layoutRootToRebuild; // typically the panel or content RectTransform
    public float extraPadding = 8f;
    private float overrideQuestionTextFieldHeight;

    private void OnEnable()
    {
        UpdateLayout();
    }

    private void OnValidate()
    {
        UpdateLayout();
    }

    // Call this after populating texts / generating answer buttons
    public void UpdateLayout()
    {
        UpdateQuestionHeight();
        UpdateExplanationHeight();
        if (updateAnswerButtons) UpdateAnswersHeight();

        // Force a rebuild so Unity re-measures with the new preferred heights
        if (layoutRootToRebuild != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRootToRebuild);
    }

    private void UpdateQuestionHeight()
    {
        if (questionTMP == null) return;

        if (questionLayoutElement == null)
            questionLayoutElement = questionTMP.GetComponent<LayoutElement>();

        if (questionLayoutElement == null)
            return;

        float targetHeight = overrideQuestionTextFieldHeight > 0f
            ? overrideQuestionTextFieldHeight
            : questionTMP.preferredHeight;

        float finalHeight = targetHeight + extraPadding;
        questionLayoutElement.preferredHeight = finalHeight;
        questionLayoutElement.minHeight = finalHeight;

        if (questionTMP.rectTransform != null)
        {
            questionTMP.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
        }
    }

    private void UpdateExplanationHeight()
    {
        if (explanationImage == null) return;

        if (explanationLayoutElement == null)
            explanationLayoutElement = explanationImage.GetComponent<LayoutElement>();

        if (explanationLayoutElement == null) return;

        float preferred = explanationImage.rectTransform.rect.height;
        explanationLayoutElement.preferredHeight = preferred + extraPadding;
    }

    public void SetQuestionTextFieldHeight(float textFieldHeight)
    {
        overrideQuestionTextFieldHeight = textFieldHeight;
        if (textFieldHeight > 0f && questionLayoutElement != null)
        {
            UpdateQuestionHeight();
        }
    }

    private void UpdateAnswersHeight()
    {
        if (answersContainer == null) return;

        for (int i = 0; i < answersContainer.childCount; i++)
        {
            var child = answersContainer.GetChild(i) as RectTransform;
            if (child == null) continue;

            var le = child.GetComponent<LayoutElement>();
            if (le == null) continue;

            // find TMP text in children
            var tmp = child.GetComponentInChildren<TextMeshProUGUI>();
            float preferred = 0f;
            if (tmp != null)
                preferred = tmp.preferredHeight;

            le.preferredHeight = Mathf.Max(minAnswerButtonHeight, preferred + extraPadding);
        }
    }
}
