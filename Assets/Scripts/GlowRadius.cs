using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BNG;

public class GlowRadius : MonoBehaviour
{
    // [SerializeField] private GameObject GlowObject;
    private BNGPlayerController playerController;
    // private Grabbable grabbableComponent;
    private float distance;
    // private Renderer objectAppearance;
    private Material[] rendererMaterials;

    private List<Material> rendMaterials;
    // private Color defaultColor;
    private InteractableGroup parentComponent;
    private bool glowAdded = false;

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
        // Debug.Log($"{this.transform.localPosition}");
        // grabbableComponent = this.GetComponent<Grabbable>();
        // objectAppearance = GetComponent<Renderer>();
        rendererMaterials = GetComponent<Renderer>().materials;
        rendMaterials = new List<Material>(rendererMaterials);
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
        
        // foreach (var mat in rendererMaterials)
        // {
        //     if (mat.shader.name == parentComponent.GlowMaterial.shader.name)
        //     {
        //         glowAdded = true;
        //         break;
        //     }
        // }
        //
        // if (!glowAdded)
        // {
        //     AddGlowMaterial(parentComponent.GlowMaterial);
        //     glowAdded = true;
        // }
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerController) return;
        
        // Debug.Log($"{playerController.transform.position}");
        // Debug.Log($"{playerController.transform.localPosition}");
        distance = Vector3.Distance(transform.position, playerController.transform.position); 
        Debug.Log(distance);

        if (distance <= 0.90f)
        {
            rendMaterials.Add(parentComponent.GlowMaterial);
            GetComponent<Renderer>().materials = rendMaterials.ToArray();
            // parentComponent.GlowMaterial.SetFloat("_Scale", 1.18f);
            // objectAppearance.material.color = Color.cyan;
        }
        else
        {
            rendMaterials.Remove(parentComponent.GlowMaterial);
            GetComponent<Renderer>().materials = rendMaterials.ToArray();
            // parentComponent.GlowMaterial.SetFloat("_Scale", 1f);
            // objectAppearance.material.color = defaultColor;
        }
        
        // rendererMaterials = rendMaterials.ToArray();
        
    }
}
