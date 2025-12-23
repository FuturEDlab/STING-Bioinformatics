using UnityEngine;
using System.Collections.Generic;
// using System.Linq;
using BNG;

public class InteractableGroup: MonoBehaviour
{
    [SerializeField] private bool applyNeededChildComponents;
    [SerializeField] private bool applyPickUp;
    [SerializeField] private PickUpGroup group;
    // [SerializeField] private Material glowMaterial;

    // public Material GlowMaterial => glowMaterial;
    

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        if (!applyNeededChildComponents) return;
        
        if (!applyPickUp) return;
        
        foreach (Transform child in transform)
        {
            group.AddDefault_PickUpComponents(child);
        }
        // string allNames = string.Join(", ", members.Select(m => m.name));
        // Debug.Log($"[{allNames}]");
        //
        // foreach (Transform child in transform)
        // {
        //     // members.Add(child.gameObject);
        //     Debug.Log(child.gameObject.name);
        // }
    }
    
    private void Awake()
    {

        // Grabbable grabbableObject;
        // Transform grabPoint;
        // Rigidbody rigidObject;
        if (!applyPickUp) return;
        
        foreach (Transform child in transform)
        {
            group.AddDefault_PickUpComponents(child);
        }
    }
}