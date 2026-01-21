using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using BNG;
using UnityEditor;

public enum InteractInput
{
    XButton,
    AButton,
    YButton,
    BButton,
    LeftTrigger,
    RightTrigger,
    LeftGrip,
    RightGrip,
}

public class Interact : MonoBehaviour
{
    // [SerializeField] private GameObject GlowObject;
    // [SerializeField] private Component interactionBehaviour;
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private float MaxGlowDistance = 2f;
    [SerializeField] private float detectionAngle = 25f;
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform rightHandAnchor;
    // [SerializeField] private InteractInput interactButton;
    // [SerializeField] private List<MonoScript> customInteractScripts;
    [SerializeField] private List<GameObject> otherObjectsAffected;
    
    private IInteraction interaction;
    private BNGPlayerController playerController;
    private float distance;
    private Material[] rendererMaterials;
    private Renderer renderer;
    private Collider objectCollider;
    private List<Material> rendMaterials;
    
    // private Color defaultColor;
    private InteractableGroup parentComponent;
    private bool glowAdded;
    private Vector3 closestPoint;
    private Transform cam;
    private bool isFacingCamera;
    private bool isLeftHandNear;
    private bool isRightHandNear;
    private bool isHandNear => isLeftHandNear || isRightHandNear;
    // private Vector3 defaultScale;
    // private bool isNewScale;

    // void ChangeObjectScale()
    // {
    //     if (isNewScale)
    //     {
    //         transform.localScale = defaultScale;
    //         isNewScale = false;
    //     }
    //     else
    //     {
    //         transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
    //         isNewScale = true;
    //     }
    // }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // interaction = interactionBehaviour as IInteraction;
        // defaultScale = transform.localScale;
        // Debug.Log(interactButton);
        Debug.Log($"{this.transform.position}");
        parentComponent = GetComponentInParent<InteractableGroup>();
        Debug.Log($"{parentComponent.GlowMaterial}");
        
        objectCollider = GetComponent<Collider>();
        renderer = GetComponent<Renderer>();
        rendMaterials = new List<Material>(renderer.materials);
        // Debug.Log($"{parentComponent.GlowMaterial.GetFloat("_Scale")}");
        
        // if (InputBridge.Instance != null)
        // {
        //     playerController = InputBridge.Instance.GetComponentInChildren<BNGPlayerController>();
        // }

        // if (Camera.main != null)
        // {
        //     cam = Camera.main.transform;
        // }
    }

    // Update is called once per frame
    void Update()
    {
        // if (!playerController) return;
        
        if (!playerController && InputBridge.Instance)
        {
            playerController = InputBridge.Instance.GetComponentInChildren<BNGPlayerController>();
        }

        if (!cam && Camera.main)
        {
            cam = Camera.main.transform;
        }
        
        if (!playerController || !cam) return;

        // if (!leftHandAnchor) return;
        // Debug.Log(Camera.main.transform.rotation.eulerAngles);
        
        // Debug.Log($"{playerController.transform.position}");
        // Debug.Log($"{playerController.transform.localPosition}");
        if (leftHandAnchor != null)
        {
            Debug.Log($"left hand pos: {leftHandAnchor.position}");
            float leftDist = Vector3.Distance(transform.position, leftHandAnchor.position);
            Debug.Log($"left dist: {leftDist}");
        }

        closestPoint = objectCollider.ClosestPoint(playerController.transform.position);
        // closestPoint = objectCollider.ClosestPoint(cam.position);
        // distance = Vector3.Distance(transform.position, playerController.transform.position);
        distance = Vector3.Distance(closestPoint, playerController.transform.position); 
        // distance = Vector3.Distance(closestPoint, cam.position); 
        Debug.Log(distance);
        // Vector3 directionToObject = (transform.position - playerController.transform.position).normalized;
        Vector3 directionToObject = (transform.position - cam.position).normalized;
        // float angle = Vector3.Angle(playerController.transform.forward, directionToObject);
        float angle = Vector3.Angle(cam.forward, directionToObject);

        if (distance <= MaxGlowDistance && !glowAdded)
        {
            rendMaterials.Add(parentComponent.GlowMaterial);
            renderer.materials = rendMaterials.ToArray();
            glowAdded = true;
        }
        else if (distance > MaxGlowDistance && glowAdded)
        {
            rendMaterials.Remove(parentComponent.GlowMaterial);
            renderer.materials = rendMaterials.ToArray();
            glowAdded = false;
        }
        
        // if (!glowAdded) return;

        // isFacingCamera = IsLookingAtObject();
        isFacingCamera = angle <= detectionAngle;
        Debug.Log($"Angle: {angle} | Facing: {isFacingCamera}");
        
        if (Input.GetKeyDown(KeyCode.L) && glowAdded && isHandNear)
        // if (InputBridge.Instance.XButtonDown && glowAdded)
        // if (IsInteractButtonPressed() && glowAdded && isHandNear)
        {
            onInteract?.Invoke();
            // interaction?.Interactt();
            // ChangeObjectScale();
        }
        
    }
    
    private bool IsInteractButtonPressed()
    {
        switch (parentComponent.InteractButton)
        {
            case InteractInput.XButton:
                return InputBridge.Instance.XButtonDown;
            case InteractInput.AButton:
                return InputBridge.Instance.AButtonDown;
            case InteractInput.YButton:
                return InputBridge.Instance.YButtonDown;
            case InteractInput.BButton:
                return InputBridge.Instance.BButtonDown;
            case InteractInput.RightTrigger:
                return InputBridge.Instance.RightTriggerDown; // or whichever trigger
            case InteractInput.LeftTrigger:
                return InputBridge.Instance.LeftTriggerDown;
            case InteractInput.RightGrip:
                return InputBridge.Instance.RightGripDown; // or whichever grip
            case InteractInput.LeftGrip:
                return InputBridge.Instance.LeftGripDown;
            default:
                return false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LeftHand"))
        {
            isLeftHandNear = true;
            Debug.Log($"Left Hand Near: {isLeftHandNear}");
        }
        else if (other.CompareTag("RightHand"))
        {
            isRightHandNear = true;
            Debug.Log($"Right Hand Near: {isRightHandNear}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LeftHand"))
        {
            isLeftHandNear = false;
            Debug.Log($"Left Hand Near: {isLeftHandNear}");
        }
        else if (other.CompareTag("RightHand"))
        {
            isRightHandNear = false;
            Debug.Log($"Right Hand Near: {isRightHandNear}");
        }
    }
    
    private bool IsLookingAtObject()
    {
        if (objectCollider == null) return false;
    
        // Use collider bounds (works even without renderer)
        Bounds bounds = objectCollider.bounds;
        Ray ray = new Ray(playerController.transform.position, playerController.transform.forward);
    
        // Check if ray intersects the bounds
        float distancee;
        return bounds.IntersectRay(ray, out distancee) && distance <= MaxGlowDistance;
        
        
        
        
        // RaycastHit hit;
        //
        // if (Physics.Raycast(ray, out hit, MaxGlowDistance))
        // {
        //     // Check if the raycast hit THIS object or any of its children
        //     return hit.collider.gameObject == gameObject || 
        //            hit.collider.transform.IsChildOf(transform);
        // }
        //
        // return false;
    }
}
