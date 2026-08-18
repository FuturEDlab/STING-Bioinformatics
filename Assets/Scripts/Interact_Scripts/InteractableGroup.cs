using UnityEngine;
using System;
using BNG;

[ExecuteAlways]
public class InteractableGroup: MonoBehaviour
{
    [Tooltip("Keep this turned on when interactable item is also allowed to be picked up")]
    [SerializeField] private bool applyPickUp;
    [Tooltip("This is for extracting the PickUpGroup script from the parent GameObject" +
             " (PickUpItems)")]
    [SerializeField] private PickUpGroup group;
    [SerializeField] private Material glowMaterial;
    [SerializeField] private InteractInput interactButton;

    public Material GlowMaterial => glowMaterial;
    public InteractInput InteractButton => interactButton;

    private void Remove_PickUpComponents(Transform Child)
    {
        // "Action" type is for functions that return void (nothing)
        Action<UnityEngine.Object> DestroyComponent;
        
        if (Application.isPlaying)
        {
            // Destroy is meant for run time only
            DestroyComponent = Destroy;
        }
        else
        {
            // DestroyImmediate is meant for edit mode only
            DestroyComponent = DestroyImmediate;
        }
        
        if (Child.TryGetComponent(out GrabbableRingHelper ringHelperObject))
        {
            DestroyComponent(ringHelperObject);
        }
            
        if (Child.TryGetComponent(out Grabbable grabbableObject))
        {
            DestroyComponent(grabbableObject);
        }
            
        if (Child.TryGetComponent(out GrabStability stableObject))
        {
            DestroyComponent(stableObject);
        }
            
        if (Child.TryGetComponent(out Rigidbody rigidObject))
        {
            DestroyComponent(rigidObject);
        }
    }
    
#if UNITY_EDITOR
    private void OnTransformChildrenChanged()
    {
        if (Application.isPlaying) return;
        
        // Iterate through all child transforms of this GameObject
        foreach (Transform child in transform)
        {
            if (applyPickUp && (group != null))
            {
                group.AddDefault_PickUpComponents(child);
            }

            if (!applyPickUp)
            {
                Remove_PickUpComponents(child);
            }
            if (child.GetComponent<Interact>() == null)
            {
                child.gameObject.AddComponent<Interact>();
            }
        }
    }
#endif
    
    private void Awake()
    {
        if (!Application.isPlaying) return;
        
        // Iterate through all child transforms of this GameObject
        foreach (Transform child in transform)
        {
            if (!applyPickUp)
            {
                Remove_PickUpComponents(child);
            }
            
            if (child.GetComponent<Interact>() == null)
            {
                child.gameObject.AddComponent<Interact>();
            }
        }
    }
}