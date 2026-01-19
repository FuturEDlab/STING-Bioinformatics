using UnityEngine;
using BNG;

[ExecuteAlways]
public class NonPickUpGroup : MonoBehaviour
{
    
#if UNITY_EDITOR
    private void OnTransformChildrenChanged()
    {
        if (Application.isPlaying) return;
        
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Interact interactComp))
            {
                DestroyImmediate(interactComp);
            }
            
            if (child.TryGetComponent(out GrabbableRingHelper ringHelperObject))
            {
                DestroyImmediate(ringHelperObject);
            }
            
            if (child.TryGetComponent(out Grabbable grabbableObject))
            {
                DestroyImmediate(grabbableObject);
            }
            
            if (child.TryGetComponent(out StableRelease stableObject))
            {
                DestroyImmediate(stableObject);
            }
            
            if (child.TryGetComponent(out Rigidbody rigidObject))
            {
                DestroyImmediate(rigidObject);
            }
        }
    }
#endif
    
    private void Awake()
    {
        if (!Application.isPlaying) return;
        
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Interact interactComp))
            {
                Destroy(interactComp);
            }
            
            if (child.TryGetComponent(out GrabbableRingHelper ringHelperObject))
            {
                Destroy(ringHelperObject);
            }
            
            if (child.TryGetComponent(out Grabbable grabbableObject))
            {
                DestroyImmediate(grabbableObject);
            }
            
            if (child.TryGetComponent(out StableRelease stableObject))
            {
                Destroy(stableObject);
            }
            
            if (child.TryGetComponent(out Rigidbody rigidObject))
            {
                Destroy(rigidObject);
            }
        }
    }
}
