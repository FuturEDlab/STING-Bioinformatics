using UnityEngine;
using BNG;

public class GlowRadius : MonoBehaviour
{
    // [SerializeField] private GameObject GlowObject;
    private BNGPlayerController playerController;
    private Grabbable grabbableComponent;
    private float distance;
    private Renderer objectAppearance;
    private Color defaultColor;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log($"{this.transform.position}");
        // Debug.Log($"{this.transform.localPosition}");
        grabbableComponent = this.GetComponent<Grabbable>();
        objectAppearance = GetComponent<Renderer>();
        defaultColor = objectAppearance.material.color;
        // grabbableComponent.RemoteGrabDistance;
        
        if (InputBridge.Instance != null)
        {
            playerController = InputBridge.Instance.GetComponentInChildren<BNGPlayerController>();
            // playerBody = InputBridge.Instance.GetComponentInChildren<BNGPlayerController>().transform;
            // Debug.Log($"{playerBody.localPosition}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerController) return;
        
        // Debug.Log($"{playerController.transform.position}");
        // Debug.Log($"{playerController.transform.localPosition}");
        distance = Vector3.Distance(transform.position, playerController.transform.position); 
        Debug.Log(distance);

        if (distance <= grabbableComponent.RemoteGrabDistance)
        {
            objectAppearance.material.color = Color.cyan;
        }
        else
        {
            objectAppearance.material.color = defaultColor;
        }
        
    }
}
