using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TeleportationActivator : MonoBehaviour
{
    public XRRayInteractor teleportInteractor;
    public XRRayInteractor rayInteractor;
    public InputActionProperty teleportActivatorAction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        teleportInteractor.gameObject.SetActive(false);
        teleportActivatorAction.action.performed += Action_performed;
        rayInteractor.uiHoverEntered.AddListener(x => DisableTeleportRay());
    }

    // Update is called once per frame
    void Update()
    {
        if (teleportActivatorAction.action.WasReleasedThisFrame())
        {
            teleportInteractor.gameObject.SetActive(false);
            if (rayInteractor != null)
            {
                rayInteractor.enabled = true;
            }
        }
    }

    private void Action_performed(InputAction.CallbackContext obj)
    {
        if (rayInteractor && rayInteractor.IsOverUIGameObject())
        {
            return;
        }
        {
            rayInteractor.enabled = false;
        }
        teleportInteractor.gameObject.SetActive(true);
    }

    public void DisableTeleportRay()
    {
        teleportInteractor.gameObject.SetActive(false);
        rayInteractor.enabled = true;
    }

}
