using UnityEngine;
using BNG;

public class StableRelease : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grabbable;
    private Collider coll;
    private bool wasHeldLastFrame;
    private PickUpGroup parentObject;
    private IgnoreColliders s;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        coll = GetComponent<Collider>();
        parentObject = GetComponentInParent<PickUpGroup>();
        // s.CollidersToIgnore.Add(pla);
        // if (grabbable)
        //     // grabbable.OnRelease.AddListener(OnReleased);
        //     // grabbable.Release;
    }

    void Update()
    {
        if (!grabbable || !rb) return;

        if (grabbable.BeingHeld)
        {
            Physics.IgnoreCollision(coll, parentObject.PlayerCollider, true);
            // coll.enabled = false;
            // coll.isTrigger = true;
            // rb.isKinematic = true;
            // rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        else
        {
            Physics.IgnoreCollision(coll, parentObject.PlayerCollider, false);
            // coll.enabled = true;
            // coll.isTrigger = false;
            // rb.isKinematic = false;
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
