using UnityEngine;
using UnityEngine.InputSystem;

public class InteractInputListener : MonoBehaviour
{
    // public InputAction interactAction;
    [SerializeField] private InputActionReference interactAction;
    private Interact currentInteractable;

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteract;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteract;
            interactAction.action.Disable();
        }
    }
    
    private void OnInteract(InputAction.CallbackContext ctx)
    {
        TryInteract();
    }

    private void TryInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interacting();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Interact interactable;
        Debug.Log($"Trigger entered: {other.name}");
        if (other.TryGetComponent(out Interact interactable))
        {
            currentInteractable = interactable;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Interact interactable;
        
        if (other.TryGetComponent(out Interact interactable) &&
            interactable == currentInteractable)
        {
            currentInteractable = null;
        }
    }
}
