using System.Collections;
using UnityEngine;

public class PatientController : MonoBehaviour
{

    // Assign the scene's ScenarioController in the Inspector.
    [SerializeField] private ScenarioController scenarioController;

    // Assign the Animator on the nurse in the Inspector.
    [SerializeField] private Animator animator;
    [SerializeField] private Renderer patientRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material anaphylaxisMaterial;
    [SerializeField] private float repeatInterval = 10f;

    [Tooltip("How long to wait for the '30 minutes later' blackout before giving up and leaving the rashes to the step they always appeared on. Only matters in a scene that has a Time Skip Card.")]
    [SerializeField] private float blackoutWaitSeconds = 5f;

    private bool isAnimating = false;

    void Start()
    {
        // First random check happens after 30 seconds,
        // then repeats every 30 seconds.
        InvokeRepeating(nameof(RandomizeNumber), repeatInterval, repeatInterval);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

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
            // Update skin to normal skin
            DisableRashes();
            StopReactToAnaphylaxis();
        }

        // Stop whatever the nurse was doing during the previous step.
        //StopCurrentAnimation();

        if (stepData == null)
        {
            return;
        }

        switch (stepData.name)
        {

            // No show wristband animation because the patient raises the wrong arm

            /*case "S1_09_Sarah_ScanWristband":
                ShowWristbandAnimation();
                break;

            case "S1_12_Sarah_ScanMethotrexate":
                StopShowWristbandAnimation();
                break;*/

            case "S1_16_Sarah_JustOverrideIt":
                LookAtNurse();
                break;
            
            case "S1_20_Sarah_TrustTheSystem":
                StopLookAtNurse();
                break;

            // The rashes belong to the half hour the player does not see. This step is the
            // "30 minutes later" blackout, so the skin changes behind the black and Mr.
            // Johnson is already covered when the view comes back.
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
        if (isAnimating)
        {
            return;
        }

        // Random.Range with ints excludes the maximum,
        // so 11 means numbers 0-10.
        int randomNumber = Random.Range(0, 2);

        Debug.Log("Random number: " + randomNumber);

        if (randomNumber == 0)
        {
            animator.SetTrigger("Left Tapping"); 
            isAnimating = true;
        }
        if (randomNumber == 1)
        {
            animator.SetTrigger("Right Tapping");
            isAnimating = true;
        }
    }

    private void ShowWristbandAnimation()
    {
        animator.SetBool("Show Wristband", true);
        isAnimating = true;
    }

    private void StopShowWristbandAnimation()
    {
        animator.SetBool("Show Wristband", false);
        isAnimating = false;
    }

    private void EnableRashes()
    {
        patientRenderer.material = anaphylaxisMaterial;
    }

    private void DisableRashes()
    {
        patientRenderer.material = normalMaterial;
    }
    
    private void ReactToAnaphylaxis()
    {
        animator.SetBool("React to Rashes", true);
        isAnimating = true;
    }

    private void StopReactToAnaphylaxis()
    {
        animator.SetBool("React to Rashes", false);
        isAnimating = false;
    }

    private void LookAtNurse()
    {
        animator.SetBool("Look at Nurse", true);
        isAnimating = true;
    }

    private void StopLookAtNurse()
    {
        animator.SetBool("Look at Nurse", false);
        isAnimating = false;
    }

    // Call this when the Left Tapping animation finishes.
    public void RandomAnimationFinished()
    {
        animator.ResetTrigger("Left Tapping");
        animator.ResetTrigger("Right Tapping");
        isAnimating = false;
    }
}
