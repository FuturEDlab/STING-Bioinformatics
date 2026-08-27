using System.Collections;
using UnityEngine;

public class PatientController : MonoBehaviour
{
    [Header("Scene References")]

    // Assign the scene's ScenarioController in the Inspector.
    [SerializeField] private ScenarioController scenarioController;

    // Assign the patient's Animator in the Inspector.
    [SerializeField] private Animator animator;

    // Assign the patient's Renderer in the Inspector.
    [SerializeField] private Renderer patientRenderer;


    [Header("Patient Materials")]

    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material anaphylaxisMaterial;


    [Header("Random Tapping")]

    // How often the script attempts to play a random tapping animation.
    [SerializeField] private float repeatInterval = 10f;


    [Header("Time Skip")]

    [Tooltip(
        "How long to wait for the '30 minutes later' blackout before giving up. " +
        "Only matters in a scene that has a Time Skip Card."
    )]
    [SerializeField] private float blackoutWaitSeconds = 5f;

    private float nextTimeScaleLog;
    private Coroutine randomAnimationCoroutine;


    // This now tracks only the random left and right tapping animations.
    private bool isAnimating;

    // Used to make debugging easier in the Console.
    private string activeAnimationReason = "None";
    private bool useLeftHand = true;


    private void OnEnable()
    {
        Debug.Log(
            "PatientController enabled. Starting random animation timer.",
            this
        );

        if (scenarioController != null)
        {
            scenarioController.StepEntered += OnScenarioStepEntered;
        }

        if (randomAnimationCoroutine != null)
        {
            StopCoroutine(randomAnimationCoroutine);
        }

        randomAnimationCoroutine = StartCoroutine(RandomAnimationLoop());
    }

    private void OnDisable()
    {
        Debug.Log(
            "PatientController disabled. Stopping random animation timer.",
            this
        );

        if (scenarioController != null)
        {
            scenarioController.StepEntered -= OnScenarioStepEntered;
        }

        if (randomAnimationCoroutine != null)
        {
            StopCoroutine(randomAnimationCoroutine);
            randomAnimationCoroutine = null;
        }

        ResetRandomAnimation();
    }

    /*private void Update()
    {
        if (Time.unscaledTime >= nextTimeScaleLog)
        {
            nextTimeScaleLog = Time.unscaledTime + 2f;

            Debug.Log(
                $"Time.timeScale: {Time.timeScale}, " +
                $"Time.time: {Time.time}, " +
                $"Time.unscaledTime: {Time.unscaledTime}",
                this
            );
        }
    }*/

    private void OnScenarioStepEntered(
        int stepIndex,
        ScenarioStepData stepData
    )
    {
        // Step 0 is the first step entered by the linear timeline.
        if (stepIndex == 0)
        {
            DisableRashes();
            StopReactToAnaphylaxis();

            // Reset random tapping in case the scenario restarted while
            // a tapping animation was playing.
            ResetRandomAnimation();
        }

        if (stepData == null)
        {
            return;
        }

        switch (stepData.name)
        {
            case "S1_16_Sarah_JustOverrideIt":
                LookAtNurse();
                break;

            case "S1_20_Sarah_TrustTheSystem":
                StopLookAtNurse();
                break;

            case "S3A_08_TimeSkip30Minutes":
                EnableRashesUnderTheFade();
                break;

            case "S3B_02_Sarah_BloodPressure":
                EnableRashes();
                ReactToAnaphylaxis();
                break;
        }
    }


    /// <summary>
    /// The time-skip step raises its beat the instant it is entered, which is the instant the
    /// fade to black *starts* — swapping the material there would pop the rashes on in front
    /// of the player. So wait for the screen to actually go black first.
    ///
    /// A scene with no time-skip card never goes black, and there the wait simply times out
    /// and leaves the rashes to S3B_02, exactly as before.
    /// </summary>
    private void EnableRashesUnderTheFade()
    {
        if (isActiveAndEnabled)
        {
            StartCoroutine(EnableRashesWhenBlack());
        }
    }

    private IEnumerator EnableRashesWhenBlack()
    {
        float deadline = Time.time + blackoutWaitSeconds;
        while (Time.time < deadline)
        {
            if (Rig.FadeAlpha >= 0.9f)
            {
                EnableRashes();
                yield break;
            }
            yield return null;
        }
    }

    private void RandomizeNumber()
    {
        if (animator == null)
        {
            Debug.LogWarning(
                "Random tapping was skipped because the patient Animator reference is missing.",
                this
            );
            return;
        }

        if (isAnimating)
        {
            Debug.LogWarning(
                "Random tapping was skipped because isAnimating is still " +
                $"true. Active animation: {activeAnimationReason}",
                this
            );

            return;
        }

        StartRandomAnimation(useLeftHand ? "Left Tapping" : "Right Tapping");
        useLeftHand = !useLeftHand;
    }

    private IEnumerator RandomAnimationLoop()
    {
        if (repeatInterval <= 0f)
        {
            Debug.LogError(
                "Random tapping was not started because repeatInterval must be greater than zero.",
                this
            );
            yield break;
        }

        while (isActiveAndEnabled)
        {
            Debug.Log(
                $"Random tapping timer waiting {repeatInterval} realtime seconds. " +
                $"isAnimating: {isAnimating}, active animation: {activeAnimationReason}",
                this
            );

            yield return new WaitForSecondsRealtime(repeatInterval);
            RandomizeNumber();
        }

        Debug.LogWarning(
            "Random tapping timer stopped because PatientController is no longer active and enabled.",
            this
        );
    }

    private void StartRandomAnimation(string triggerName)
    {
        // Set the lock before setting the trigger so another request
        // cannot start during the same frame.
        SetAnimating(true, triggerName);

        // Clear both triggers before setting the selected trigger.
        // This prevents an old trigger from still being queued.
        animator.ResetTrigger("Left Tapping");
        animator.ResetTrigger("Right Tapping");

        animator.SetTrigger(triggerName);

        Debug.Log(
            $"Started random patient animation: {triggerName}",
            this
        );
    }


    // Call this with an Animation Event near the end of both the
    // Left Tapping and Right Tapping animation clips.
    public void RandomAnimationFinished()
    {
        ResetRandomAnimation();

        Debug.Log(
            "Random animation finished. Tapping is available again.",
            this
        );
    }

    private void ResetRandomAnimation()
    {
        if (animator != null)
        {
            animator.ResetTrigger("Left Tapping");
            animator.ResetTrigger("Right Tapping");
        }

        SetAnimating(false, "Random tapping finished or reset");
    }

    private void SetAnimating(bool value, string reason)
    {
        isAnimating = value;
        activeAnimationReason = value ? reason : "None";

        Debug.Log(
            $"Patient isAnimating changed to {value}. Reason: {reason}",
            this
        );
    }


    private void ShowWristbandAnimation()
    {
        animator.SetBool("Show Wristband", true);
    }

    private void StopShowWristbandAnimation()
    {
        animator.SetBool("Show Wristband", false);
    }


    private void EnableRashes()
    {
        if (patientRenderer == null || anaphylaxisMaterial == null)
        {
            Debug.LogWarning(
                "The patient Renderer or anaphylaxis Material is missing.",
                this
            );

            return;
        }

        patientRenderer.material = anaphylaxisMaterial;
    }

    private void DisableRashes()
    {
        if (patientRenderer == null || normalMaterial == null)
        {
            Debug.LogWarning(
                "The patient Renderer or normal Material is missing.",
                this
            );

            return;
        }

        patientRenderer.material = normalMaterial;
    }


    private void ReactToAnaphylaxis()
    {
        animator.SetBool("React to Rashes", true);
    }

    private void StopReactToAnaphylaxis()
    {
        if (animator != null)
        {
            animator.SetBool("React to Rashes", false);
        }
    }


    private void LookAtNurse()
    {
        animator.SetBool("Look at Nurse", true);
    }

    private void StopLookAtNurse()
    {
        animator.SetBool("Look at Nurse", false);
    }
}