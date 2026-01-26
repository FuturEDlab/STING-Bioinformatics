using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using BNG;

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
    
    [Tooltip("Event triggered when this object is interacted with")]
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private float MaxGlowDistance = 2f;
    
    private BNGPlayerController playerController;
    private float distance;
    private Renderer renderer;
    private Collider objectCollider;
    private List<Material> rendMaterials;
    
    private InteractableGroup parentComponent;
    private bool glowAdded;
    private Vector3 closestPoint;
    
    private bool isLeftHandNear;
    private bool isRightHandNear;
    private bool isHandNear => isLeftHandNear || isRightHandNear;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        parentComponent = GetComponentInParent<InteractableGroup>();
        objectCollider = GetComponent<Collider>();
        renderer = GetComponent<Renderer>();
        rendMaterials = new List<Material>(renderer.materials);
    }

    // Update is called once per frame
    void Update()
    {
        
        if (!playerController && InputBridge.Instance)
        {
            playerController = InputBridge.Instance.GetComponentInChildren<BNGPlayerController>();
        }
        
        if (!playerController) return;

        // Calculates distance to nearest point on collider surface. -->
        // This helps prevent checking only the object's center
        closestPoint = objectCollider.ClosestPoint(playerController.transform.position);
        distance = Vector3.Distance(closestPoint, playerController.transform.position); 

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

        // At this point, we can assume that if player is outside glow radius,
        // the glow isn't present, leading to returning early since interaction
        // won't work regardless.
        if (!glowAdded) return;
        
        if (IsInteractButtonPressed() && isHandNear)
        {
            onInteract?.Invoke();
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
                return InputBridge.Instance.RightTriggerDown;
            case InteractInput.LeftTrigger:
                return InputBridge.Instance.LeftTriggerDown;
            case InteractInput.RightGrip:
                return InputBridge.Instance.RightGripDown;
            case InteractInput.LeftGrip:
                return InputBridge.Instance.LeftGripDown;
            default:
                return false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Tags LeftHand and RightHand are associated with the Grabber child objects
        // from LeftController object and RightController object (XR Rig Full Body,
        // the parent object, contains these 2 Controller objects)
        
        if (other.CompareTag("LeftHand"))
        {
            isLeftHandNear = true;
        }
        else if (other.CompareTag("RightHand"))
        {
            isRightHandNear = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Tags LeftHand and RightHand are associated with the Grabber child objects
        // from LeftController object and RightController object (XR Rig Full Body,
        // the parent object, contains these 2 controller objects)
        
        if (other.CompareTag("LeftHand"))
        {
            isLeftHandNear = false;
        }
        else if (other.CompareTag("RightHand"))
        {
            isRightHandNear = false;
        }
    }
    
}
