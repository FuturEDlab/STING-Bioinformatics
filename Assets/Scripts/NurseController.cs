using System.Collections;
using UnityEngine;

public class NurseController : MonoBehaviour
{
    // Assign the scene's ScenarioController in the Inspector.
    [SerializeField] private ScenarioController scenarioController;

    // Assign the Animator on the nurse in the Inspector.
    [SerializeField] private Animator animator;


    [Header("Scenario Start")]
    // Empty GameObject marking the nurse's starting position and facing direction.
    [SerializeField] private Transform startDestination;


    [Header("Walking")]
    // Empty GameObject placed where the nurse should walk to.
    [SerializeField] private Transform patientDestination;
    [SerializeField] private Transform midRoomDestination;

    // How fast the nurse moves forward.
    [SerializeField] private float moveSpeed = 1.5f;

    // How quickly the nurse turns toward the destination.
    [SerializeField] private float turnSpeed = 180f;

    // How close the nurse needs to get before stopping.
    [SerializeField] private float stoppingDistance = 0.1f;


    // Tracks which non-locomotion animation is currently active.
    private bool nurseIsTalking;
    private bool nurseIsShaking;

    // Tracks whether the nurse is currently walking.
    private bool nurseIsWalking;

    // Stores the active walking coroutine so it can be stopped if needed.
    private Coroutine walkCoroutine;
    private Transform activeWalkDestination;


    private void OnEnable()
    {
        // Subscribe before the scenario starts.
        if (scenarioController != null)
        {
            scenarioController.StepEntered += OnScenarioStepEntered;
        }
    }

    private void OnDisable()
    {
        // Always unsubscribe when disabled.
        if (scenarioController != null)
        {
            scenarioController.StepEntered -= OnScenarioStepEntered;
        }
    }


    private void OnScenarioStepEntered(int stepIndex, ScenarioStepData stepData)
    {
        // Step 0 is the first step entered by the linear timeline. Place the
        // nurse here without firing an Animator trigger or playing an animation.
        if (stepIndex == 0)
        {
            TeleportToStartDestination();
        }

        // Stop whatever the nurse was doing during the previous step.
        StopCurrentAnimation();

        if (stepData == null)
        {
            return;
        }

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

            case "S1_15_Sarah_ChildbearingAge":
                StartTalkingAnimation();
                break;

            case "S1_16_Sarah_JustOverrideIt":
                WalkToDestination(patientDestination);
                break;

            case "S1_20_Sarah_TrustTheSystem":
                WalkToDestination(midRoomDestination);
                break;

            case "S2_01_Sarah_NextAmoxicillin":
                StartTalkingAnimation();
                break;

            case "S2_04_Sarah_CheckHistoryNotes":
                StartTalkingAnimation();
                break;

            case "S2_06_Sarah_WhoaStop":
                StartShakingAnimation();
                break;
            
            case "S2_10_Sarah_Better":
                //StopCurrentAnimation();
                StartTalkingAnimation();
                break;

            case "S3A_01_Sarah_ScanAllopurinol":
                StartTalkingAnimation();
                break;

            case "S3A_04_Sarah_ProbablyABadFlag":
                StartTalkingAnimation();
                break;
            
            case "S3A_07_Sarah_MedsAdministered":
                StartTalkingAnimation();
                break;

            case "S3B_02_Sarah_BloodPressure":
                WalkToDestination(patientDestination);
                // StartTalkingAnimation();
                break;

            case "S3B_03_Sarah_TheAlertWasReal":
                StartShakingAnimation();
                break;

            case "S3B_04_Sarah_WeHurtHim":
                WalkToDestination(patientDestination);
                break;
        }
    }


    private void TeleportToStartDestination()
    {
        if (startDestination == null)
        {
            Debug.LogWarning(
                "[NurseController] Start Destination is missing. " +
                "Assign a Transform in the Inspector if the nurse should be " +
                "placed automatically when the scenario starts.",
                this
            );

            return;
        }

        // Copy both position and rotation so the nurse starts facing the
        // direction authored by the destination marker.
        transform.SetPositionAndRotation(
            startDestination.position,
            startDestination.rotation
        );
    }


    private bool CheckForAnimator()
    {
        if (animator == null)
        {
            Debug.LogError(
                "[NurseController] Animator reference is missing. " +
                "Assign the nurse's Animator in the Inspector."
            );

            return false;
        }

        return true;
    }

    private void StartShakingAnimation()
    {
        if (!CheckForAnimator())
        {
            return;
        }

        animator.SetTrigger("Start Shaking");

        nurseIsShaking = true;
    }

    private void StartTalkingAnimation()
    {
        if (!CheckForAnimator())
        {
            return;
        }

        animator.SetTrigger("Start Talking");

        nurseIsTalking = true;
    }


    // Use this overload when another system needs to choose the destination at runtime.
    // UnityEvents can pass a Transform argument to this public method.
    public void WalkToDestination(Transform destination)
    {
        if (!CheckForAnimator())
        {
            return;
        }

        if (destination == null)
        {
            Debug.LogError(
                "[NurseController] Walk destination is missing. " +
                "Pass a destination Transform to WalkToDestination or assign " +
                "Destination for the parameterless fallback."
            );

            return;
        }

        // Stop an existing walking coroutine if one is already running.
        if (walkCoroutine != null)
        {
            nurseIsWalking = false;
            StopCoroutine(walkCoroutine);
            walkCoroutine = null;
        }

        nurseIsWalking = true;
        activeWalkDestination = destination;

        // The coroutine pivots in place first, then starts the walking animation
        // and physically moves toward the selected destination.
        walkCoroutine = StartCoroutine(WalkToDestinationRoutine(destination));
    }


    private void StartWalkingAnimation()
    {
        // The Animator Controller uses this Bool to transition from
        // Start Walk into the looping Walk Cycle state.
        animator.SetBool("Walking", true);
        animator.SetTrigger("Start Walking");
    }


    private IEnumerator WalkToDestinationRoutine(Transform destination)
    {
        // Pivot before translating so a destination behind the nurse does not
        // cause sideways movement or collisions with nearby objects.
        while (nurseIsWalking)
        {
            Vector3 directionToDestination = destination.position - transform.position;
            directionToDestination.y = 0f;

            if (directionToDestination.sqrMagnitude <= stoppingDistance * stoppingDistance)
                break;

            Quaternion targetRotation = Quaternion.LookRotation(directionToDestination.normalized);
            if (Quaternion.Angle(transform.rotation, targetRotation) <= 1f)
                break;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (!nurseIsWalking)
        {
            StopWalking();
            yield break;
        }

        StartWalkingAnimation();

        while (nurseIsWalking)
        {
            // Direction from the nurse to the destination.
            Vector3 direction = destination.position - transform.position;

            // Ignore height differences.
            // We only want the nurse turning left/right.
            direction.y = 0f;

            // Check whether the nurse has reached the destination.
            if (direction.sqrMagnitude <= stoppingDistance * stoppingDistance)
            {
                break;
            }

            // Figure out which direction the nurse should face.
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

            // Smoothly turn toward the destination.
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            // Move forward in the direction the nurse is CURRENTLY facing.
            transform.position += transform.forward * moveSpeed * Time.deltaTime;

            // Wait until the next frame before continuing.
            yield return null;
        }

        // Continue turning smoothly after arriving until the nurse matches the
        // destination marker's authored rotation. This avoids a sudden snap at
        // the end of the walk.
        while (nurseIsWalking && Quaternion.Angle(transform.rotation, destination.rotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                destination.rotation,
                turnSpeed * Time.deltaTime
            );

            yield return null;
        }

        StopWalking();
    }


    private void StopWalking()
    {
        nurseIsWalking = false;
        walkCoroutine = null;
        activeWalkDestination = null;

        if (!CheckForAnimator())
        {
            return;
        }

        // Clear the Bool so the Animator can leave Walk Cycle and play
        // the configured stopping animation.
        animator.ResetTrigger("Start Walking");
        animator.SetBool("Walking", false);
        animator.SetTrigger("Stop Walking");
    }


    public void StopCurrentAnimation()
    {
        if (nurseIsWalking)
        {
            TeleportToActiveWalkDestination();
        }

        if (!CheckForAnimator())
        {
            return;
        }

        // Stop talking if the nurse was talking.
        if (nurseIsTalking)
        {
            animator.ResetTrigger("Start Talking");
            animator.SetTrigger("Stop Talking");

            nurseIsTalking = false;
        }

        // Stop shaking with its matching trigger rather than sending a
        // talking stop trigger for a different animation state.
        if (nurseIsShaking)
        {
            animator.ResetTrigger("Start Shaking");
            animator.SetTrigger("Stop Shaking");

            nurseIsShaking = false;
        }

        // Stop walking if the nurse was walking.
        if (nurseIsWalking)
        {
            if (walkCoroutine != null)
            {
                StopCoroutine(walkCoroutine);
                walkCoroutine = null;
            }

            nurseIsWalking = false;
            activeWalkDestination = null;

            // Clear the walking state even when the movement is interrupted
            // by a scenario step change rather than reaching the destination.
            animator.ResetTrigger("Start Walking");
            animator.SetBool("Walking", false);
            animator.SetTrigger("Stop Walking");
        }
    }


    private void TeleportToActiveWalkDestination()
    {
        if (activeWalkDestination == null)
        {
            return;
        }

        transform.SetPositionAndRotation(
            activeWalkDestination.position,
            activeWalkDestination.rotation
        );
    }
}