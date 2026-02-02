using UnityEngine;
using BNG;

public class GrabStability : MonoBehaviour
{
    // [SerializeField] private GameObject ground;
    private Rigidbody rb;
    private Grabbable grabbable;
    private Collider coll;
    private bool wasHeldLastFrame;
    private PickUpGroup parentObject;
    private int originalLayer;
    private GameObject groundObj;
    private float defaultYposition;

    private void CheckObjBelowFloor()
    {
        // float fallThreshold = groundObj.transform.position.y - 5;
        float floorHeight = groundObj.transform.position.y;
        // Vector3 pos = transform.position;
        
        // Check if object is intersecting with player
        if (coll.bounds.Intersects(parentObject.PlayerCollider.bounds))
        {
            // Calculate spawn distance based on object and player size
            float objectRadius = Mathf.Max(coll.bounds.extents.x, coll.bounds.extents.z);
            float playerRadius = Mathf.Max(parentObject.PlayerCollider.bounds.extents.x,
                parentObject.PlayerCollider.bounds.extents.z);
            float spawnDistance = objectRadius + playerRadius + 0.5f; // Add buffer for safety
        
            // Spawn in front of player
            Vector3 playerForward = parentObject.PlayerCollider.transform.forward;
            Vector3 spawnPos = parentObject.PlayerCollider.transform.position + playerForward * spawnDistance;
            spawnPos.y = floorHeight + coll.bounds.extents.y; // Place on floor
            transform.position = spawnPos;
        
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // Check if object's bottom is below floor (and not in player)
        else if (coll.bounds.min.y < floorHeight)
        {
            // Snap bottom of object to floor surface
            Vector3 pos = transform.position;
            pos.y = floorHeight + coll.bounds.extents.y;
            transform.position = pos;
        
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // if (pos.y < fallThreshold)
        // {
        //     pos.y = defaultYposition;
        //     transform.position = pos;
        //     // transform.position.y = defaultYposition;
        // }
    }
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        coll = GetComponent<Collider>();
        parentObject = GetComponentInParent<PickUpGroup>();
        originalLayer = gameObject.layer;
        groundObj = parentObject.Ground;
        defaultYposition = transform.position.y;
    }

    void Update()
    {
        // if (groundObj)
        // {
        //     Debug.Log($"{transform.name}: {transform.position.y}, Ground: {ground.transform.position.y}");
        // }

        if (!grabbable || !rb) return;
        if (!groundObj) return;
        
        Debug.Log($"{transform.name}: {transform.position.y}, Ground: {groundObj.transform.position.y}");

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
            CheckObjBelowFloor();
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 10f);
        }
        
        wasHeldLastFrame = grabbable.BeingHeld;
    }
}
