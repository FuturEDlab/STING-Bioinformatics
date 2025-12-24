using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BNG;

public class GlowRadius : MonoBehaviour
{
    // [SerializeField] private GameObject GlowObject;
    [SerializeField] private float MaxGlowDistance = .90f;
    
    private BNGPlayerController playerController;
    // private Grabbable grabbableComponent;
    private float distance;
    // private Renderer objectAppearance;
    private Material[] rendererMaterials;
    private Renderer renderer;
    private Collider objectCollider;

    private List<Material> rendMaterials;
    // private Color defaultColor;
    private InteractableGroup parentComponent;
    private bool glowAdded = false;
    private Vector3 closestPoint;

    void AddGlowMaterial(Material glowMat)
    {
        List<Material> materials = new List<Material>(rendererMaterials);
        materials.Add(glowMat);
        GetComponent<Renderer>().materials = materials.ToArray();
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log($"{this.transform.position}");
        parentComponent = GetComponentInParent<InteractableGroup>();
        Debug.Log($"{parentComponent.GlowMaterial}");
        objectCollider = GetComponent<Collider>();
        // Debug.Log($"{this.transform.localPosition}");
        // grabbableComponent = this.GetComponent<Grabbable>();
        // objectAppearance = GetComponent<Renderer>();
        // rendererMaterials = GetComponent<Renderer>().materials;
        renderer = GetComponent<Renderer>();
        rendMaterials = new List<Material>(renderer.materials);
        // Debug.Log($"{objectAppearance.materials.}");
        Debug.Log($"{parentComponent.GlowMaterial.GetFloat("_Scale")}");
        // defaultColor = objectAppearance.material.color;
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
        closestPoint = objectCollider.ClosestPoint(playerController.transform.position);
        // distance = Vector3.Distance(transform.position, playerController.transform.position); 
        distance = Vector3.Distance(closestPoint, playerController.transform.position); 
        Debug.Log(distance);

        if (distance <= MaxGlowDistance && !glowAdded)
        {
            rendMaterials.Add(parentComponent.GlowMaterial);
            // GetComponent<Renderer>().materials = rendMaterials.ToArray();
            renderer.materials = rendMaterials.ToArray();
            glowAdded = true;
            // parentComponent.GlowMaterial.SetFloat("_Scale", 1.18f);
            // objectAppearance.material.color = Color.cyan;
        }
        else if (distance > MaxGlowDistance && glowAdded)
        {
            rendMaterials.Remove(parentComponent.GlowMaterial);
            // GetComponent<Renderer>().materials = rendMaterials.ToArray();
            renderer.materials = rendMaterials.ToArray();
            glowAdded = false;
            // parentComponent.GlowMaterial.SetFloat("_Scale", 1f);
            // objectAppearance.material.color = defaultColor;
        }
        
        // renderer.materials = rendMaterials.ToArray();
        // GetComponent<Renderer>().materials = rendMaterials.ToArray();
        
    }
}
