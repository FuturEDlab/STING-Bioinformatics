// using System;
using UnityEngine;
using BNG;

[ExecuteAlways]
public class PickUpGroup : MonoBehaviour
{
    
    [SerializeField] private Collider playerCollider;
    
    public Collider PlayerCollider => playerCollider;

    public void AddDefault_PickUpComponents(Transform Child)
    {
        Grabbable grabbableObject;
        GrabbableRingHelper ringHelper;
        Rigidbody rigidObject;

        if (Child.GetComponent<Grabbable>() == null)
        {
            grabbableObject = Child.gameObject.AddComponent<Grabbable>();
            grabbableObject.GrabPhysics = GrabPhysics.PhysicsJoint;
            grabbableObject.RemoteGrabbable = false;
        }

        if (Child.GetComponent<GrabbableRingHelper>() == null)
        {
            ringHelper = Child.gameObject.AddComponent<GrabbableRingHelper>();
            ringHelper.RingHelperScale = 0.8f;
        }
        
        if (Child.GetComponent<StableRelease>() == null)
        {
            Child.gameObject.AddComponent<StableRelease>();
        }
            
        if (Child.GetComponent<Rigidbody>() == null)
        {
            rigidObject = Child.gameObject.AddComponent<Rigidbody>();
            rigidObject.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidObject.angularDamping = 0.5f;
        }
    }
    
#if UNITY_EDITOR
    private void OnTransformChildrenChanged()
    {
        if (Application.isPlaying) return;
        
        foreach (Transform child in transform)
        {
            if (child.GetComponent<InteractableGroup>() != null)
            {
                continue;
            }
            
            if (child.TryGetComponent(out Interact interactComp))
            {
                DestroyImmediate(interactComp);
            }
            
            AddDefault_PickUpComponents(child);
            
        }
    }
#endif

}
