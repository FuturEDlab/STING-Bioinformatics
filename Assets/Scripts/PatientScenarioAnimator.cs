using System.Collections;
using UnityEngine;

public class PatientScenarioAnimator : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private ScenarioController scenarioController;
    [SerializeField] private Animator animator;
    [SerializeField] private Renderer patientRenderer;

    [Header("Patient Materials")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material anaphylaxisMaterial;

    [Header("Random Tapping")]
    [SerializeField] private float tapInterval = 10f;
    [SerializeField] private float tapAnimationDuration = 2f;

    private Coroutine tappingCoroutine;
    private bool tapInProgress;

    private void OnEnable()
    {
        if (scenarioController != null)
        {
            scenarioController.StepEntered += OnScenarioStepEntered;
        }

        if (tappingCoroutine != null)
        {
            StopCoroutine(tappingCoroutine);
        }

        tappingCoroutine = StartCoroutine(RandomTappingLoop());
    }

    private void OnDisable()
    {
        if (scenarioController != null)
        {
            scenarioController.StepEntered -= OnScenarioStepEntered;
        }

        if (tappingCoroutine != null)
        {
            StopCoroutine(tappingCoroutine);
            tappingCoroutine = null;
        }

        tapInProgress = false;
        ResetTapTriggers();
    }

    private void OnScenarioStepEntered(int stepIndex, ScenarioStepData stepData)
    {
        if (stepIndex == 0)
        {
            DisableRashes();
            StopReactToAnaphylaxis();
            StopLookAtNurse();
            ResetTapTriggers();
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
                //EnableRashesUnderTheFade();
                break;

            case "S3B_02_Sarah_BloodPressure":
                EnableRashes();
                ReactToAnaphylaxis();
                break;
        }
    }

    private IEnumerator RandomTappingLoop()
    {
        if (animator == null)
        {
            Debug.LogError("PatientScenarioAnimator cannot start random tapping because Animator is missing.", this);
            yield break;
        }

        if (tapInterval <= 0f)
        {
            Debug.LogError("PatientScenarioAnimator cannot start random tapping because tapInterval must be greater than zero.", this);
            yield break;
        }

        while (isActiveAndEnabled)
        {
            yield return new WaitForSecondsRealtime(tapInterval);

            if (tapInProgress)
            {
                Debug.LogWarning("PatientScenarioAnimator skipped a tap because the previous tap is still in progress.", this);
                continue;
            }

            string triggerName = Random.value < 0.5f ? "Left Tapping" : "Right Tapping";
            ResetTapTriggers();
            animator.SetTrigger(triggerName);
            tapInProgress = true;

            Debug.Log($"PatientScenarioAnimator triggered {triggerName}.", this);

            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, tapAnimationDuration));

            ResetTapTriggers();
            tapInProgress = false;
            Debug.Log("PatientScenarioAnimator finished the tap window.", this);
        }
    }

    private void ResetTapTriggers()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger("Left Tapping");
        animator.ResetTrigger("Right Tapping");
        tapInProgress = false;
    }

    private void EnableRashes()
    {
        if (patientRenderer == null || anaphylaxisMaterial == null)
        {
            Debug.LogWarning("PatientScenarioAnimator cannot enable rashes because the Renderer or anaphylaxis Material is missing.", this);
            return;
        }

        patientRenderer.material = anaphylaxisMaterial;
    }

    private void DisableRashes()
    {
        if (patientRenderer == null || normalMaterial == null)
        {
            Debug.LogWarning("PatientScenarioAnimator cannot disable rashes because the Renderer or normal Material is missing.", this);
            return;
        }

        patientRenderer.material = normalMaterial;
    }

    private void ReactToAnaphylaxis()
    {
        if (animator != null)
        {
            animator.SetBool("React to Rashes", true);
        }
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
        if (animator != null)
        {
            animator.SetBool("Look at Nurse", true);
        }
    }

    private void StopLookAtNurse()
    {
        if (animator != null)
        {
            animator.SetBool("Look at Nurse", false);
        }
    }
}
