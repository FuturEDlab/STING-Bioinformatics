using UnityEngine;
using System.Collections.Generic;
// using System.Linq;
using BNG;

public class InteractableGroup: MonoBehaviour
{
    [SerializeField] private bool applyNeededChildComponents;
    [SerializeField] private bool applyPickUp;
    [SerializeField] private PickUpGroup group;
    [SerializeField] private Material glowMaterial;

    public Material GlowMaterial => glowMaterial;

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        if (!applyNeededChildComponents) return;
        
        // if (!applyPickUp) return;
        
        foreach (Transform child in transform)
        {
            if (applyPickUp && group != null)
            {
                group.AddDefault_PickUpComponents(child);
            }
            if (child.GetComponent<Interact>() == null)
            {
                child.gameObject.AddComponent<Interact>();
            }
        }
        
    }
    
    private void Awake()
    {
    
        // Grabbable grabbableObject;
        // Transform grabPoint;
        // Rigidbody rigidObject;
        // if (!applyPickUp) return;
        
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Interact>() == null)
            {
                child.gameObject.AddComponent<Interact>();
            }
            // group.AddDefault_PickUpComponents(child);
        }
    }
}