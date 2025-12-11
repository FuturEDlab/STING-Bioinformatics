using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;
using BNG;

public class InteractableGroup: MonoBehaviour
{
    [SerializeField] private bool applyNeededChildComponents;

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        Grabbable grabbableObject;
        Rigidbody rigidObject;
        if (!applyNeededChildComponents) return;
        
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Grabbable>() == null)
            {
                grabbableObject = child.gameObject.AddComponent<Grabbable>();
                grabbableObject.GrabPhysics = GrabPhysics.PhysicsJoint;
                grabbableObject.RemoteGrabbable = true;
            }
            
            if (child.GetComponent<Rigidbody>() == null)
            {
                rigidObject = child.gameObject.AddComponent<Rigidbody>();
                rigidObject.collisionDetectionMode = CollisionDetectionMode.Continuous;
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
        Rigidbody rigidObject;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Grabbable>() == null)
            {
                grabbableObject = child.gameObject.AddComponent<Grabbable>();
                grabbableObject.GrabPhysics = GrabPhysics.PhysicsJoint;
                grabbableObject.RemoteGrabbable = true;
            }
            
            if (child.GetComponent<Rigidbody>() == null)
            {
                rigidObject = child.gameObject.AddComponent<Rigidbody>();
                rigidObject.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }
    }
}