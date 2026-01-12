using UnityEngine;
using BNG;

public class StableRelease : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grabbable;
    private Collider coll;
    private bool wasHeldLastFrame;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        coll = GetComponent<Collider>();
        // if (grabbable)
        //     // grabbable.OnRelease.AddListener(OnReleased);
        //     // grabbable.Release;
    }

    void Update()
    {
        if (!grabbable || !rb) return;

        if (grabbable.BeingHeld)
        {
            coll.enabled = false;
            // coll.isTrigger = true;
            // rb.isKinematic = true;
            // rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        else
        {
            coll.enabled = true;
            // coll.isTrigger = false;
        }
        
        if (wasHeldLastFrame && !grabbable.BeingHeld)
        {
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 10f);
        }
        
        wasHeldLastFrame = grabbable.BeingHeld;
    }

    // void OnReleased(Grabbable g)
    // {
    //     rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 10f);
    //     // rb.angularDrag = 5f;
    // }
}
