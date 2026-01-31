using UnityEngine;
using BNG;

public class GrabStability : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grabbable;
    private Collider coll;
    private bool wasHeldLastFrame;
    private PickUpGroup parentObject;
    private int originalLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        coll = GetComponent<Collider>();
        parentObject = GetComponentInParent<PickUpGroup>();
        originalLayer = gameObject.layer;
    }

    void Update()
    {
        if (!grabbable || !rb) return;

        if (grabbable.BeingHeld)
        {
            Physics.IgnoreCollision(coll, parentObject.PlayerCollider, true);
            gameObject.layer = LayerMask.NameToLayer("Grabb");
        }
        else
        {
            Physics.IgnoreCollision(coll, parentObject.PlayerCollider, false);
            gameObject.layer = originalLayer;
        }
        
        // This is right after releasing object to prevent excessive spinning
        if (wasHeldLastFrame && !grabbable.BeingHeld)
        {
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 10f);
        }
        
        wasHeldLastFrame = grabbable.BeingHeld;
    }
}
