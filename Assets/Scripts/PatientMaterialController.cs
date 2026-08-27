using System.Collections;
using UnityEngine;

public class PatientMaterialController : MonoBehaviour
{
    [Header("Scene References")]

    // Assign the scene's ScenarioController in the Inspector.
    [SerializeField] private ScenarioController scenarioController;

    // Assign the patient's Renderer in the Inspector.
    [SerializeField] private Renderer patientRenderer;


    [Header("Patient Materials")]

    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material anaphylaxisMaterial;

    [Header("Time Skip")]

    [Tooltip(
        "How long to wait for the '30 minutes later' blackout before giving up. " +
        "Only matters in a scene that has a Time Skip Card."
    )]
    [SerializeField] private float blackoutWaitSeconds = 5f;

    private void OnEnable()
    {

        if (scenarioController != null)
        {
            scenarioController.StepEntered += OnScenarioStepEntered;
        }
    }

    private void OnDisable()
    {
        if (scenarioController != null)
        {
            scenarioController.StepEntered -= OnScenarioStepEntered;
        }
    }

    private void OnScenarioStepEntered(
        int stepIndex,
        ScenarioStepData stepData
    )
    {
        // Step 0 is the first step entered by the linear timeline.
        if (stepIndex == 0)
        {
            DisableRashes();
        }

        if (stepData == null)
        {
            return;
        }

        switch (stepData.name)
        {
            case "S3A_08_TimeSkip30Minutes":
                EnableRashesUnderTheFade();
                break;

            case "S3B_02_Sarah_BloodPressure":
                EnableRashes();
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

}