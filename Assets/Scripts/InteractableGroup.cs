using UnityEngine;
using System;
using BNG;

[ExecuteAlways]
public class InteractableGroup: MonoBehaviour
{
    [SerializeField] private bool applyPickUp;
    [SerializeField] private PickUpGroup group;
    [SerializeField] private Material glowMaterial;
    [SerializeField] private InteractInput interactButton;

    public Material GlowMaterial => glowMaterial;
    public InteractInput InteractButton => interactButton;

    private void Remove_PickUpComponents(Transform Child)
    {
        Action<UnityEngine.Object> DestroyComponent;
        
        if (Application.isPlaying)
        {
            DestroyComponent = Destroy;
        }
        else
        {
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
            
        if (Child.TryGetComponent(out StableRelease stableObject))
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
        Debug.Log("OnTransform chichi fyeeeee!!!");
        if (Application.isPlaying) return;
        
        foreach (Transform child in transform)
        {
            if (applyPickUp && group != null)
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