using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using BNG;

public class GrabStability : MonoBehaviour
{
    private Rigidbody rb;
    private Grabbable grabbable;
    private Collider coll;
    private Collider playerColl;
    private bool wasHeldLastFrame;
    private PickUpGroup parentObject;
    private int originalLayer;
    private GameObject groundObj;
    private bool wasAboveTable;
    private RaycastHit hit;
    
    private void CheckTableIntersection()
    {
        // This function determines whether an object should drop below table
        // or snap back on top of table.
        
        // Find nearby table colliders
        Collider[] nearbyColliders = Physics.OverlapBox(coll.bounds.center, coll.bounds.extents, transform.rotation);
    
        foreach (Collider nearbyCol in nearbyColliders)
        {
            Transform isParentCollider;
            bool isParentTable = false;

            isParentCollider = nearbyCol.transform.parent;
            if (isParentCollider != null)
            {
                isParentTable = isParentCollider.CompareTag("Table");
            }
            
            bool isTable = nearbyCol.CompareTag("Table") || isParentTable;
        
            if (!isTable) continue;

            // When released object isn't intersecting with any tables in scene
            if (!coll.bounds.Intersects(nearbyCol.bounds)) continue;
            
            float tableSurfaceY = nearbyCol.bounds.max.y;
            float objectCenterY = coll.bounds.center.y;
        
            Vector3 pos = transform.position;
            pos.y = tableSurfaceY + coll.bounds.extents.y;
        
            if (objectCenterY >= tableSurfaceY)
            {
                // Snap to table
                transform.position = pos;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                break;
            }
            
            // Center is below - teleport right side of player
            float objectRadius = Mathf.Max(coll.bounds.extents.x, coll.bounds.extents.z);
            float playerRadius = Mathf.Max(playerColl.bounds.extents.x, playerColl.bounds.extents.z);
            float spawnDistance = objectRadius + playerRadius + 0.5f;
            
            Vector3 playerRight = playerColl.transform.right;
            Vector3 spawnPos = playerColl.transform.position + playerRight * spawnDistance;
            spawnPos.y = groundObj.transform.position.y + coll.bounds.extents.y; // Place on floor
            transform.position = spawnPos;
            
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            break;
            
        }
    }
    
    private void CorrectObjectPosition()
    {
        // This function makes sure objects doesn't fall below floor
        // and prevents physic glitches from occuring when released
        // object is inside the player's body.
        
        float floorHeight = groundObj.transform.position.y;
        
        // Check if object is intersecting with player
        if (coll.bounds.Intersects(playerColl.bounds))
        {
            // Calculate spawn distance based on object and player size
            float objectRadius = Mathf.Max(coll.bounds.extents.x, coll.bounds.extents.z);
            float playerRadius = Mathf.Max(playerColl.bounds.extents.x, playerColl.bounds.extents.z);
            float spawnDistance = objectRadius + playerRadius + 0.5f; // Add buffer for safety
        
            // Spawn right side of player
            Vector3 playerRight = playerColl.transform.right;
            Vector3 spawnPos = playerColl.transform.position + playerRight * spawnDistance;
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
    }

    private IEnumerator ReleasePhysics()
    {
        // Keep collisions ignored for one physics step
        yield return new WaitForFixedUpdate();
        
        Physics.IgnoreCollision(coll, playerColl, false);
        gameObject.layer = originalLayer;
    }
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        coll = GetComponent<Collider>();
        parentObject = GetComponentInParent<PickUpGroup>();
        originalLayer = gameObject.layer;
        playerColl = parentObject.PlayerCollider;
        groundObj = parentObject.Ground;
    }

    void Update()
    {

        if (!grabbable || !rb) return;
        if (!groundObj) return;
        
        // started grabbing object
        if (!wasHeldLastFrame && grabbable.BeingHeld)
        {
            Physics.IgnoreCollision(coll, playerColl, true);
            gameObject.layer = LayerMask.NameToLayer("Grabb");
        }

        // This is right when the object gets released
        if (wasHeldLastFrame && !grabbable.BeingHeld)
        {
            CheckTableIntersection();
            CorrectObjectPosition();
            
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 10f); // Prevents
            // excessive spinning when object lands on floor

            StartCoroutine(ReleasePhysics());
        }
        
        wasHeldLastFrame = grabbable.BeingHeld;
    }
}
