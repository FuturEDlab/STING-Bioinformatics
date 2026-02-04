using System.Collections.Generic;
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
    private bool wasAboveTable;

    private RaycastHit hit;
    // private Gr

    // private void EnableCollidableObjects()
    // {
    //     HashSet<Collider> collidableObjSet = GrabCollisionsOn.uniqueCollidableObjs;
    //     Debug.Log($"self collider: {coll.name}");
    //
    //     foreach (Collider obj in collidableObjSet)
    //     {
    //         Debug.Log($"held collidable collider: {obj}");
    //         Physics.IgnoreCollision(coll, obj, false);
    //     }
    // }
    
    private void CheckTableIntersection()
    {
        if (!wasAboveTable) return;
        
        RaycastHit table = hit;
        Collider tableCollider = table.collider;
        
        // Check if object is intersecting with this table
        if (coll.bounds.Intersects(tableCollider.bounds))
        {
            // Get table surface height (top of the table)
            float tableSurfaceY = tableCollider.bounds.max.y;

            // Check if object center is mostly above or below table surface
            float objectCenterY = coll.bounds.center.y;

            if (objectCenterY > tableSurfaceY)
            {
                // Mostly above - snap to table surface
                Vector3 pos = transform.position;
                pos.y = tableSurfaceY + coll.bounds.extents.y;
                transform.position = pos;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            // If mostly below, let it fall (do nothing)
        }
    }
    
    private void CheckObjBelowFloor()
    {
        // float fallThreshold = groundObj.transform.position.y - 5;
        float floorHeight = groundObj.transform.position.y;
        // Vector3 pos = transform.position;
        
        // Check if object is intersecting with player
        if (coll.bounds.Intersects(parentObject.PlayerCollider.bounds))
        {
            // Physics.IgnoreCollision(coll, parentObject.PlayerCollider, true);
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
            // Physics.IgnoreCollision(coll, parentObject.PlayerCollider, false);
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
        // Physics.IgnoreCollision(coll, parentObject.PlayerCollider, false);
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
        // private RayCastHit hit;
    }

    void Update()
    {
        // if (groundObj)
        // {
        //     Debug.Log($"{transform.name}: {transform.position.y}, Ground: {ground.transform.position.y}");
        // }
        // Debug.Log($"{transform.name}: {transform.position.y}");
        // if (transform.CompareTag("Table"))
        // {
        //     Debug.Log("is a Table");
        //     return;
        // }

        if (Physics.Raycast(coll.bounds.center, Vector3.down, out hit, 10))
        {
            // Check if hit object has "Table" tag
            if (hit.transform.CompareTag("Table") || (hit.collider.transform.parent != null && hit.collider.transform.parent.CompareTag("Table")))
            {
                // Debug.Log($"{transform.name} hit [{hit.transform.name}]");
                // Debug.Log($"{hit.transform.name}: {hit.transform.position.y}");
                wasAboveTable = true;
                // tableCollider = hit.collider;
                // tableSurfaceY = hit.point.y;
                // return true;
            }
        }

        if (!grabbable || !rb) return;
        if (!groundObj) return;
        
        // Debug.Log($"{transform.name}: {transform.position.y}, Ground: {groundObj.transform.position.y}");

        if (grabbable.BeingHeld)
        {
            Physics.IgnoreCollision(coll, parentObject.PlayerCollider, true);
            gameObject.layer = LayerMask.NameToLayer("Grabb");
            // EnableCollidableObjects();
        }
        else
        {
            Physics.IgnoreCollision(coll, parentObject.PlayerCollider, false);
            // Don't reset layer if this is a collidable object
            // if (!GrabCollisionsOn.uniqueCollidableObjs.Contains(gameObject))
            // { 
            //     gameObject.layer = originalLayer;
            // }
            // else if (GrabCollisionsOn.uniqueCollidableObjs.Contains(gameObject))
            // {
            //     gameObject.layer = LayerMask.NameToLayer("Collidable");
            // }
            gameObject.layer = originalLayer;
        }
        
        // This is right after releasing object to prevent excessive spinning
        if (wasHeldLastFrame && !grabbable.BeingHeld)
        {
            CheckTableIntersection();
            CheckObjBelowFloor();
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 10f);
            Physics.IgnoreCollision(coll, parentObject.PlayerCollider, true); // At 4-4:30 PM Feb 4,
            // test player jump glitch when the object barely touches player's stomach area!! 
        }
        
        wasHeldLastFrame = grabbable.BeingHeld;
    }
    
    // void OnTriggerEnter(Collider other)
    // {
    //     // Tags LeftHand and RightHand are associated with the Grabber child objects
    //     // from LeftController object and RightController object (XR Rig Full Body,
    //     // the parent object, contains these 2 Controller objects)
    //     
    //     if (other.CompareTag("Table"))
    //     {
    //         Debug.Log($"Table touched {other.name}");
    //         // isLeftHandNear = true;
    //     }
    //     else if (other.CompareTag("Table"))
    //     {
    //         Debug.Log($"Table did not touch {other.name}");
    //         // isRightHandNear = true;
    //     }
    // }
}
