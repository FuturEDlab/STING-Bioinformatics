using UnityEngine;
using BNG;

public class GrabStability : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grabbable;
    private Collider coll;
    private bool wasHeldLastFrame;
    private PickUpGroup parentObject;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        coll = GetComponent<Collider>();
        parentObject = GetComponentInParent<PickUpGroup>();
    }

    void Update()
    {
        if (!grabbable || !rb) return;

        if (grabbable.BeingHeld)
        {
            Physics.IgnoreCollision(coll, parentObject.PlayerCollider, true);
        }
        else
        {
            Physics.IgnoreCollision(coll, parentObject.PlayerCollider, false);
        }
        
        // This is right after releasing object to prevent excessive spinning
        if (wasHeldLastFrame && !grabbable.BeingHeld)
        {
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 10f);
        }
        
        wasHeldLastFrame = grabbable.BeingHeld;
    }
}
