using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class IgnorePlayerPhysicsWhileHeld : MonoBehaviour
{
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    [SerializeField] private Collider[] playerColliders;

    private Collider[] objectColliders;

    private void Awake()
    {
        objectColliders = GetComponentsInChildren<Collider>();

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        SetPlayerCollision(false);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        SetPlayerCollision(true);
    }

    private void SetPlayerCollision(bool shouldCollide)
    {
        foreach (Collider objectCollider in objectColliders)
        {
            foreach (Collider playerCollider in playerColliders)
            {
                Physics.IgnoreCollision(
                    objectCollider,
                    playerCollider,
                    !shouldCollide
                );
            }
        }
    }
}