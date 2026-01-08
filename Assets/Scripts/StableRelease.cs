using UnityEngine;
using BNG;

public class StableRelease : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grabbable;
    private bool wasHeldLastFrame;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();

        // if (grabbable)
        //     // grabbable.OnRelease.AddListener(OnReleased);
        //     // grabbable.Release;
    }

    void Update()
    {
        if (!grabbable || !rb) return;
        
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
