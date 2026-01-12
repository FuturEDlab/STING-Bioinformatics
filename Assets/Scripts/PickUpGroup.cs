using UnityEngine;
using System.Collections.Generic;
// using System.Linq;
using BNG;
using Unity.VisualScripting;

public class PickUpGroup : MonoBehaviour
{
    [SerializeField] private bool applyNeededChildComponents;
    // private int grabLayer;

    public void AddDefault_PickUpComponents(Transform Child)
    {
        Grabbable grabbableObject;
        GrabbableRingHelper ringHelper;
        // StableRelease releasedObject;
        // Transform grabPoint;
        Rigidbody rigidObject;
        
        // grabPoint = Child.transform.Find("GrabPoint");
        if (Child.GetComponent<Grabbable>() == null)
        {
            grabbableObject = Child.gameObject.AddComponent<Grabbable>();
            grabbableObject.GrabPhysics = GrabPhysics.PhysicsJoint;
            // grabbableObject.Grabtype.
            grabbableObject.RemoteGrabbable = true;
            // grabLayer = LayerMask.NameToLayer("Grabb");
            // if (grabLayer != 1)
            // {
            //     Child.gameObject.layer = grabLayer;
            // }

            // if (grabbableObject.GrabPoints == null) 
            // {
            //     grabbableObject.GrabPoints = new List<Transform>();
            // }

            // if (grabPoint != null)
            // {
            //     grabbableObject.GrabPoints.Add(grabPoint);
            // }
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

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        // Grabbable grabbableObject;
        // Transform grabPoint;
        // Rigidbody rigidObject;
        if (!applyNeededChildComponents) return;
        
        foreach (Transform child in transform)
        {
            if (child.GetComponent<InteractableGroup>() != null)
            {
                continue;
            }
            
            AddDefault_PickUpComponents(child);
        }
        
    }
    
    // private void Awake()
    // {
    //
    //     // Grabbable grabbableObject;
    //     // Transform grabPoint;
    //     // Rigidbody rigidObject;
    //     foreach (Transform child in transform)
    //     {
    //         if (child.GetComponent<InteractableGroup>() != null)
    //         {
    //             continue;
    //         }
    //         
    //         AddDefault_PickUpComponents(child);
    //     }
    // }
}
