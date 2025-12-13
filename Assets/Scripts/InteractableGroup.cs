using UnityEngine;
using System.Collections.Generic;
// using System.Linq;
using BNG;

public class InteractableGroup: MonoBehaviour
{
    [SerializeField] private bool applyNeededChildComponents;

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        Grabbable grabbableObject;
        Transform grabPoint;
        Rigidbody rigidObject;
        if (!applyNeededChildComponents) return;
        
        foreach (Transform child in transform)
        {
            grabPoint = child.transform.Find("GrabPoint");
            if (child.GetComponent<Grabbable>() == null)
            {
                grabbableObject = child.gameObject.AddComponent<Grabbable>();
                grabbableObject.GrabPhysics = GrabPhysics.PhysicsJoint;
                grabbableObject.RemoteGrabbable = true;

                if (grabbableObject.GrabPoints == null) 
                {
                    grabbableObject.GrabPoints = new List<Transform>();
                }

                if (grabPoint != null)
                {
                    grabbableObject.GrabPoints.Add(grabPoint);
                }
            }
            
            if (child.GetComponent<Rigidbody>() == null)
            {
                rigidObject = child.gameObject.AddComponent<Rigidbody>();
                rigidObject.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            
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

        Grabbable grabbableObject;
        Transform grabPoint;
        Rigidbody rigidObject;
        foreach (Transform child in transform)
        {
            grabPoint = child.transform.Find("GrabPoint");
            if (child.GetComponent<Grabbable>() == null)
            {
                grabbableObject = child.gameObject.AddComponent<Grabbable>();
                grabbableObject.GrabPhysics = GrabPhysics.PhysicsJoint;
                grabbableObject.RemoteGrabbable = true;

                if (grabbableObject.GrabPoints == null) 
                {
                    grabbableObject.GrabPoints = new List<Transform>();
                }
                
                if (grabPoint != null)
                {
                    grabbableObject.GrabPoints.Add(grabPoint);
                }
            }
            
            if (child.GetComponent<Rigidbody>() == null)
            {
                rigidObject = child.gameObject.AddComponent<Rigidbody>();
                rigidObject.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
    }
}