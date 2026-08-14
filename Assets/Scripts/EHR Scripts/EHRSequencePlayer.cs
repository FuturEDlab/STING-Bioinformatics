using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EHRSequencePlayer : MonoBehaviour
{
    public enum StepConditionType
    {
        Time,
        Action
    }

    [System.Serializable]
    public class SequenceStep
    {
        [Header("Step Name")]
        public string stepName = "Step";

        [Header("Image")]
        public Sprite image;

        [Header("Advance Condition")]
        public StepConditionType conditionType = StepConditionType.Time;
        public float duration = 3f;
        public string actionName = "Next";

        [Header("Scene Object During This Step")]
        public GameObject sceneObject;
        public bool showSceneObject;
    }

    [Header("Target Sprite Renderer")]
    public SpriteRenderer targetRenderer;

    [Header("Sequence")]
    public List<SequenceStep> steps = new List<SequenceStep>();
    public bool autoStartOnEnable = false;
    public bool loopSequence = false;

    private int currentIndex = -1;
    private Coroutine timerRoutine;
    private GameObject currentDisplayedObject;

    private void OnEnable()
    {
        if (autoStartOnEnable)
        {
            StartSequence();
        }
    }

    private void OnDisable()
    {
        StopTimer();
        HideCurrentSceneObject();
    }

    public void StartSequence()
    {
        currentIndex = -1;
        StopTimer();
        HideCurrentSceneObject();
        Advance();
    }

    public void Advance()
    {
        if (steps == null || steps.Count == 0)
        {
            return;
        }

        if (currentIndex >= 0)
        {
            HideCurrentSceneObject();
        }

        currentIndex++;

        if (currentIndex >= steps.Count)
        {
            if (loopSequence)
            {
                currentIndex = 0;
            }
            else
            {
                currentIndex = steps.Count - 1;
                return;
            }
        }

        ShowCurrentStep();
    }

    public void TriggerAction(string actionName)
    {
        if (steps == null || steps.Count == 0 || currentIndex < 0)
        {
            return;
        }

        SequenceStep currentStep = steps[currentIndex];

        if (currentStep.conditionType != StepConditionType.Action)
        {
            return;
        }

        if (string.Equals(currentStep.actionName, actionName, System.StringComparison.Ordinal))
        {
            Advance();
        }
    }

    private void ShowCurrentStep()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning("EHRSequencePlayer: No SpriteRenderer has been assigned.");
            return;
        }

        if (currentIndex < 0 || currentIndex >= steps.Count)
        {
            return;
        }

        SequenceStep currentStep = steps[currentIndex];

        if (currentStep.image != null)
        {
            targetRenderer.sprite = currentStep.image;
        }
        else
        {
            targetRenderer.sprite = null;
        }

        if (currentStep.sceneObject != null)
        {
            if (currentStep.showSceneObject)
            {
                currentStep.sceneObject.SetActive(true);
                currentDisplayedObject = currentStep.sceneObject;
            }
            else
            {
                currentStep.sceneObject.SetActive(false);
                currentDisplayedObject = null;
            }
        }
        else
        {
            currentDisplayedObject = null;
        }

        if (currentStep.conditionType == StepConditionType.Time)
        {
            StartTimer(currentStep.duration);
        }
        else
        {
            StopTimer();
        }
    }

    private void StartTimer(float duration)
    {
        StopTimer();

        if (duration <= 0f)
        {
            Advance();
            return;
        }

        timerRoutine = StartCoroutine(WaitForDuration(duration));
    }

    private IEnumerator WaitForDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        Advance();
    }

    private void StopTimer()
    {
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }
    }

    private void HideCurrentSceneObject()
    {
        if (currentDisplayedObject != null)
        {
            currentDisplayedObject.SetActive(false);
            currentDisplayedObject = null;
        }
    }

    private void OnValidate()
    {
        if (steps == null)
        {
            return;
        }

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] == null)
            {
                continue;
            }

            if (steps[i].duration < 0f)
            {
                steps[i].duration = 0f;
            }
        }
    }
}
