using UnityEngine;

public class NurseController : MonoBehaviour
{
    // Assign the scene's ScenarioController in the Inspector.
    [SerializeField] private ScenarioController scenarioController;

    // Assign the Animator on the nurse in the Inspector.
    [SerializeField] private Animator animator;

    // Tracks whether this script started an animation that should be stopped
    // when the scenario enters another step.
    private bool nurseAnimationIsActive;

    private void OnEnable()
    {
        // Subscribe before the scenario starts.
        // OnEnable is preferable to Start because ScenarioController may
        // enter its first step during its own Start method.
        if (scenarioController != null)
        {
            scenarioController.StepEntered += OnScenarioStepEntered;
        }
    }

    private void OnDisable()
    {
        // Always unsubscribe when disabled.
        // This prevents duplicate subscriptions if the nurse is disabled
        // and enabled again during testing.
        if (scenarioController != null)
        {
            scenarioController.StepEntered -= OnScenarioStepEntered;
        }
    }

    private void OnScenarioStepEntered(int stepIndex, ScenarioStepData stepData)
    {
        // StepEntered fires every time the linear timeline changes steps.
        // Stop the previous nurse action before deciding what the new step does.
        StopCurrentAnimation();

        // stepData is the ScriptableObject asset currently being entered.
        // Its name matches the asset name shown in the Project window.
        if (stepData == null)
        {
            return;
        }

        // Use the scenario asset name to choose the nurse behavior.
        switch (stepData.name)
        {
            case "S1_04_Sarah_Greeting":
                StartTalkingAnimation();
                break;

            case "S1_05_Sarah_ScanWorkflow":
                StartTalkingAnimation();
                break;

            case "S1_06_Sarah_PickUpScanner":
                StartTalkingAnimation();
                break;

            case "S1_09_Sarah_ScanWristband":
                StartTalkingAnimation();
                break;

            case "S1_12_Sarah_ScanMethotrexate":
                StartTalkingAnimation();
                break;

            /*case "S1_05_Sarah_ScanWorkflow":
                StartScanWorkflowAnimation();
                break;

            case "S1_06_Sarah_PickUpScanner":
                PickUpScanner();
                break;*/
        }
    }

    private bool CheckForAnimator()
    {
        if (animator == null)
        {
            Debug.LogError("[NurseController] Animator reference is missing. Assign the nurse's Animator in the Inspector.");
            return false;
        }
        return true;
    }

    private void StartTalkingAnimation()
    {
        // This trigger must exist in the Nurse Animator Controller.
        if (!CheckForAnimator())
        {
            return;
        }

        animator.ResetTrigger("Stop Talking");
        animator.SetTrigger("Start Talking");
        nurseAnimationIsActive = true;
    }

    public void StopCurrentAnimation()
    {
        if (!nurseAnimationIsActive || !CheckForAnimator())
        {
            return;
        }

        // This trigger should transition the Animator back to the nurse's
        // standing/idle state.
        animator.ResetTrigger("Start Talking");
        animator.SetTrigger("Stop Talking");
        nurseAnimationIsActive = false;
    }

}
