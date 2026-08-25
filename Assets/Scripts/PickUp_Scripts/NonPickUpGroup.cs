using UnityEngine;
using System;
using BNG;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[ExecuteAlways]
public class NonPickUpGroup : MonoBehaviour
{
    private void RemoveComponents(Transform child, Action<UnityEngine.Object> DestroyRef)
    {
        if (child.TryGetComponent(out Interact interactComp))
        {
            DestroyRef(interactComp);
        }
            
        if (child.TryGetComponent(out GrabbableRingHelper ringHelperObject))
        {
            DestroyRef(ringHelperObject);
        }
            
        if (child.TryGetComponent(out Grabbable grabbableObject))
        {
            DestroyRef(grabbableObject);
        }

        // The XRI equivalent, for children added while a new-hands scene is open.
        if (child.TryGetComponent(out XRGrabInteractable xrGrabbable))
        {
            DestroyRef(xrGrabbable);
        }


        if (child.TryGetComponent(out GrabStability stableObject))
        {
            DestroyRef(stableObject);
        }
            
        if (child.TryGetComponent(out Rigidbody rigidObject))
        {
            DestroyRef(rigidObject);
        }
    }
    
#if UNITY_EDITOR
    private void OnTransformChildrenChanged()
    {
        if (Application.isPlaying) return;

        Action<UnityEngine.Object> DestroyComponent = DestroyImmediate;
        
        // Iterate through all child transforms of this GameObject
        foreach (Transform child in transform)
        {
            RemoveComponents(child, DestroyComponent);
        }
    }
#endif
    
    private void Awake()
    {
        if (!Application.isPlaying) return;
        
        Action<UnityEngine.Object> DestroyComponent = Destroy;
        
        foreach (Transform child in transform)
        {
            RemoveComponents(child, DestroyComponent);
        }
    }
}
