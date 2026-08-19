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
        public Animator animator;
        public string iconAnimatorTrigger = "";
    }

    [Header("Target Sprite Renderer")]
    public SpriteRenderer targetRenderer;

    [Header("Sequence")]
    public List<SequenceStep> steps = new List<SequenceStep>();
    public bool autoStartOnEnable = false;
    public bool loopSequence = false;

    [Header("Scenario Control")]
    [Tooltip("On: the Scenario Controller decides when the screen changes. Step timers never run and a button press no longer advances the screen by itself - the terminal moves only when GoToState is called, normally by the EHRScenarioBridge. Off: the sequence plays through on its own, which is the standalone demo behaviour.")]
    public bool scenarioDriven = false;

    [Header("Default Animation")]
    public string defaultAnimatorTrigger = "";

    private int currentIndex = -1;
    private Coroutine timerRoutine;
    private GameObject currentDisplayedObject;
    private Animator lastTriggeredAnimator;

    /// <summary>
    /// Raised for every TriggerAction call, including ones the screen currently showing
    /// ignores. EHRScenarioBridge listens here and reports the press to the scenario, so a
    /// button on the terminal can satisfy a scenario gate even when the terminal itself is
    /// not the thing driving the story forward.
    /// </summary>
    public event System.Action<string> ActionTriggered;

    /// <summary>Index of the screen showing right now, or -1 before the sequence starts.</summary>
    public int CurrentIndex => currentIndex;

    public int StepCount => steps != null ? steps.Count : 0;

    /// <summary>Step Name of the screen showing right now; empty before the sequence starts.</summary>
    public string CurrentStateName =>
        (steps != null && currentIndex >= 0 && currentIndex < steps.Count && steps[currentIndex] != null)
            ? steps[currentIndex].stepName
            : string.Empty;

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
        lastTriggeredAnimator = null;
    }

    public void StartSequence()
    {
        currentIndex = -1;
        lastTriggeredAnimator = null;
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

        int next = currentIndex + 1;

        if (next >= steps.Count)
        {
            if (!loopSequence)
            {
                // End of the sequence: stay on the last screen.
                currentIndex = steps.Count - 1;
                return;
            }

            next = 0;
        }

        SwitchTo(next);
    }

    public void TriggerAction(string actionName)
    {
        // Reported first and unconditionally: the scenario may be waiting on this press even
        // when the screen currently showing is not the one that consumes it.
        ActionTriggered?.Invoke(actionName);

        if (steps == null || steps.Count == 0 || currentIndex < 0)
        {
            return;
        }

        if (scenarioDriven)
        {
            // The scenario decides what comes next. Advancing here as well would give the
            // screen a second, competing driver.
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

    /// <summary>
    /// Switch the screen to the step with this Step Name. Returns void so it can be wired
    /// straight into a SceneEventRelay response or any other UnityEvent. An unknown name is
    /// reported rather than ignored, because a typo here is otherwise completely silent.
    /// </summary>
    public void GoToState(string stateName)
    {
        TryGoToState(stateName);
    }

    /// <summary>GoToState for callers that want to know whether the name resolved.</summary>
    public bool TryGoToState(string stateName)
    {
        int index = IndexOfState(stateName);

        if (index < 0)
        {
            Debug.LogWarning($"EHRSequencePlayer: '{name}' has no step named '{stateName}', so the screen did not change. Check the Step Name spelling.", this);
            return false;
        }

        GoToStateIndex(index);
        return true;
    }

    /// <summary>Switch the screen to a step by index.</summary>
    public void GoToStateIndex(int index)
    {
        if (steps == null || index < 0 || index >= steps.Count)
        {
            return;
        }

        StopTimer();

        // Re-cueing the screen that is already up does nothing further, so a channel raised
        // twice cannot restart whatever that screen is showing.
        if (index == currentIndex)
        {
            return;
        }

        SwitchTo(index);
    }

    /// <summary>
    /// The one way the screen ever changes. The scene object is switched off only when the
    /// screen we are moving to does not use the same one - the teratogenic alert stays up
    /// across the alert screen and the override screen, and switching it off and on again
    /// between them would rebind its Animator and lose the pop-up it had already played.
    /// </summary>
    private void SwitchTo(int index)
    {
        SequenceStep next = steps[index];
        GameObject nextObject = (next != null && next.showSceneObject) ? next.sceneObject : null;

        if (currentDisplayedObject != null && currentDisplayedObject != nextObject)
        {
            HideCurrentSceneObject();
        }

        currentIndex = index;
        ShowCurrentStep();
    }

    /// <summary>Index of the step with this Step Name, or -1. Case and padding insensitive.</summary>
    public int IndexOfState(string stateName)
    {
        if (steps == null || string.IsNullOrWhiteSpace(stateName))
        {
            return -1;
        }

        string wanted = stateName.Trim();

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] == null || steps[i].stepName == null)
            {
                continue;
            }

            if (string.Equals(steps[i].stepName.Trim(), wanted, System.StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public bool HasState(string stateName) => IndexOfState(stateName) >= 0;

    /// <summary>True when some step is set to advance on this action name.</summary>
    public bool HasAction(string actionName)
    {
        if (steps == null || string.IsNullOrWhiteSpace(actionName))
        {
            return false;
        }

        string wanted = actionName.Trim();

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] == null || steps[i].actionName == null)
            {
                continue;
            }

            if (steps[i].conditionType == StepConditionType.Action &&
                string.Equals(steps[i].actionName.Trim(), wanted, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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

        if (currentStep.showSceneObject && currentStep.sceneObject != null)
        {
            currentStep.sceneObject.SetActive(true);
            currentDisplayedObject = currentStep.sceneObject;

            string triggerToFire = currentStep.iconAnimatorTrigger;
            if (string.IsNullOrWhiteSpace(triggerToFire))
            {
                triggerToFire = defaultAnimatorTrigger;
            }

            if (!string.IsNullOrWhiteSpace(triggerToFire))
            {
                Animator animator = currentStep.animator;
                if (animator == null)
                {
                    animator = currentStep.sceneObject.GetComponent<Animator>();
                }

                if (animator != null && animator != lastTriggeredAnimator)
                {
                    animator.SetTrigger(triggerToFire);
                    lastTriggeredAnimator = animator;
                }
            }
        }
        else
        {
            currentDisplayedObject = null;
            lastTriggeredAnimator = null;
        }

        // A timer is the stand-in for "hold this while someone talks". Once the scenario is
        // driving, the line of dialogue itself decides how long that is.
        if (currentStep.conditionType == StepConditionType.Time && !scenarioDriven)
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
