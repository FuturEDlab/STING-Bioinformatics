using System.Collections.Generic;
using System.Linq;
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
    // [SerializeField] private GameObject GlowObject;
    // [SerializeField] private Component interactionBehaviour;
    [SerializeField] private UnityEvent onInteract;
    [SerializeField] private float MaxGlowDistance = .90f;
    // [SerializeField] private InteractInput interactButton;
    
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
        
        if (InputBridge.Instance != null)
        {
            playerController = InputBridge.Instance.GetComponentInChildren<BNGPlayerController>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerController) return;
        
        // Debug.Log($"{playerController.transform.position}");
        // Debug.Log($"{playerController.transform.localPosition}");
        closestPoint = objectCollider.ClosestPoint(playerController.transform.position);
        distance = Vector3.Distance(closestPoint, playerController.transform.position); 
        Debug.Log(distance);

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
        
        // if (Input.GetKeyDown(KeyCode.L) && glowAdded)
        // if (InputBridge.Instance.XButtonDown && glowAdded)
        if (IsInteractButtonPressed() && glowAdded)
        {
            onInteract.Invoke();
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
}
