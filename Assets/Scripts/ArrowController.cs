using System;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    public enum LookTargetChoice
    {
        TargetOne,
        TargetTwo
    }

    [Serializable]
    public class StepLookTarget
    {
        [Tooltip("Exact ScenarioStepData name, for example S1_14_EHR_MethoAlert.")]
        public string stepName;

        [Tooltip("Which assigned look target this scenario step uses.")]
        public LookTargetChoice target = LookTargetChoice.TargetOne;
    }

    [Header("Scene References")]

    // Assign the scene's ScenarioController in the Inspector.
    [SerializeField] private ScenarioController scenarioController;
    [Tooltip("The scenario task channel. Any completed user task hides an active arrow immediately.")]
    [SerializeField] private StringGameEvent taskChannel;
    [Tooltip("The right-pointed arrow. Existing arrow reference; used when the target is to the player's right.")]
    [SerializeField] private GameObject rightArrowObject;
    [Tooltip("The left-pointed arrow. Used when the target is to the player's left.")]
    [SerializeField] private GameObject leftArrowObject;

    [Header("Look Target")]
    [Tooltip("The object the player should look toward. The arrow hides once the player's head is aimed within the angle below.")]
    [SerializeField] private Transform lookTarget;

    [Tooltip("The second object the player may need to look toward.")]
    [SerializeField] private Transform secondLookTarget;

    [Tooltip("Choose which look target each scenario step uses. Steps not listed here use the first target.")]
    [SerializeField] private List<StepLookTarget> stepLookTargets = new List<StepLookTarget>();

    [Tooltip("Maximum angle between the player's head direction and the target for the arrow to hide.")]
    [Range(1f, 45f)]
    [SerializeField] private float targetLookAngle = 15f;

    [Tooltip("Use the target object's visible/collision bounds center instead of its Transform pivot.")]
    [SerializeField] private bool useTargetBoundsCenter = true;

    [Tooltip("Optional local-space adjustment for the point the player should look at.")]
    [SerializeField] private Vector3 targetAimOffset;

    private Transform head;
    private Transform activeLookTarget;

    private void Awake()
    {
        if (rightArrowObject != null && rightArrowObject == leftArrowObject)
            leftArrowObject = FindSeparateArrow("Left Arrow");

        if (rightArrowObject != null && rightArrowObject.GetComponent<ArrowHeadFollow>() == null)
            rightArrowObject.AddComponent<ArrowHeadFollow>();
        if (leftArrowObject != null && leftArrowObject.GetComponent<ArrowHeadFollow>() == null)
            leftArrowObject.AddComponent<ArrowHeadFollow>();
        HideArrow();

        if (scenarioController != null)
        {
            scenarioController.StepEntered += OnScenarioStepEntered;
        }

        if (taskChannel != null)
            taskChannel.Subscribe(OnTaskRaised);
    }

    private void OnDestroy()
    {
        if (scenarioController != null)
        {
            scenarioController.StepEntered -= OnScenarioStepEntered;
        }

        if (taskChannel != null)
            taskChannel.Unsubscribe(OnTaskRaised);
    }

    private void OnTaskRaised(string taskId)
    {
        if (activeLookTarget != null && (IsArrowActive(rightArrowObject) || IsArrowActive(leftArrowObject)))
            HideArrow();
    }

    private void Update()
    {
        if (activeLookTarget == null || (!IsArrowActive(rightArrowObject) && !IsArrowActive(leftArrowObject)))
            return;

        if (!ResolveHead())
            return;

        Vector3 targetPosition = GetTargetPosition(activeLookTarget);
        Vector3 directionToTarget = targetPosition - head.position;
        if (directionToTarget.sqrMagnitude < 0.0001f)
            return;

        if (Vector3.Angle(head.forward, directionToTarget) <= targetLookAngle)
            HideArrow();
    }

    private Vector3 GetTargetPosition(Transform target)
    {
        Vector3 position = target.position;

        if (useTargetBoundsCenter)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds();
            bool hasBounds = false;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (!hasBounds)
                {
                    bounds = colliders[i].bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(colliders[i].bounds);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(renderers[i].bounds);
            }

            if (hasBounds)
                position = bounds.center;
        }

        return position + target.TransformVector(targetAimOffset);
    }

    private void OnScenarioStepEntered(
        int stepIndex,
        ScenarioStepData stepData
    )
    {
        // Step 0 is the first step entered by the linear timeline.
        if (stepIndex == 0)
        {
            HideArrow();
        }

        if (stepData == null || (rightArrowObject == null && leftArrowObject == null))
        {
            return;
        }

        switch (stepData.name)
        {
            case "S1_14_EHR_MethoAlert":
                ShowArrow(TargetForStep(stepData.name));
                break;

            case "S3A_03_EHR_Contraindication":
                ShowArrow(TargetForStep(stepData.name));
                break;

            case "S4_01_OpenAssessment":
                ShowArrow(TargetForStep(stepData.name));
                break;
        }
    }

    private Transform TargetForStep(string stepName)
    {
        LookTargetChoice choice = LookTargetChoice.TargetOne;

        for (int i = 0; i < stepLookTargets.Count; i++)
        {
            StepLookTarget mapping = stepLookTargets[i];
            if (mapping != null && !string.IsNullOrWhiteSpace(mapping.stepName) &&
                string.Equals(mapping.stepName.Trim(), stepName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                choice = mapping.target;
                break;
            }
        }

        return choice == LookTargetChoice.TargetTwo ? secondLookTarget : lookTarget;
    }

    private void ShowArrow(Transform target)
    {
        activeLookTarget = target;

        if (target == null)
        {
            HideArrow();
            return;
        }

        ResolveHead();
        if (head == null)
        {
            ShowRightArrow();
            return;
        }

        Vector3 directionToTarget = GetTargetPosition(target) - head.position;
        bool targetIsLeft = Vector3.Dot(head.right, directionToTarget) < 0f;

        if (targetIsLeft)
        {
            SetActive(rightArrowObject, false);
            SetActive(leftArrowObject, true);
        }
        else
        {
            ShowRightArrow();
        }
    }

    private void HideArrow()
    {
        activeLookTarget = null;
        SetActive(rightArrowObject, false);
        SetActive(leftArrowObject, false);
    }

    private void ShowRightArrow()
    {
        SetActive(leftArrowObject, false);
        SetActive(rightArrowObject, true);
    }

    private static bool IsArrowActive(GameObject arrow)
    {
        return arrow != null && arrow.activeInHierarchy;
    }

    private static void SetActive(GameObject arrow, bool active)
    {
        if (arrow != null)
            arrow.SetActive(active);
    }

    private GameObject FindSeparateArrow(string objectName)
    {
        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform candidate = sceneTransforms[i];
            if (candidate.name == objectName && candidate.gameObject != rightArrowObject)
                return candidate.gameObject;
        }

        return null;
    }

    private bool ResolveHead()
    {
        if (head != null)
            return true;

        head = Rig.Head;
        if (head == null && Camera.main != null)
            head = Camera.main.transform;

        return head != null;
    }

}
