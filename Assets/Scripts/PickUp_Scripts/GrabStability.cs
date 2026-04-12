using System;
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
    private List<Collider> ignoreObjsTemp;
    private bool wasHeldLastFrame;
    private PickUpGroup parentObject;
    private int originalLayer;
    private GameObject groundObj;
    private bool wasAboveTable;
    private RaycastHit hit;
    private float floorHeight;
    private float posY_Placement;
    Dictionary<Collider, bool> inTriggerDict = new Dictionary<Collider, bool>();
    private List<Collider> tables;
    private bool belowTableSurface;
    private Collider interactedTable;
    
    private void CheckTableIntersection()
    {
        // This function determines whether an object should drop below table
        // or snap back on top of table.

        // bool isTable;
        
        // Find nearby table colliders
        
        // Collider[] nearbyColliders = Physics.OverlapBox(coll.bounds.center, coll.bounds.extents, transform.rotation);
    
        foreach (Collider tableColl in tables)
        {
            // isTable = tableColl.CompareTag("Table");
            //
            // if (!isTable) continue;

            // When released object isn't intersecting with any tables in scene
            if (!coll.bounds.Intersects(tableColl.bounds)) continue;
            
            // tableColl.isTrigger = true;
            
            float tableSurfaceY = tableColl.bounds.max.y;
            float objectCenterY = coll.bounds.center.y;
        
            Vector3 pos = transform.position;
            pos.y = tableSurfaceY + coll.bounds.size.y;
        
            if (objectCenterY >= tableSurfaceY)
            {
                // Snap to table
                transform.position = pos;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                break;
            }

            // tableColl.isTrigger = true;
            belowTableSurface = true;
            interactedTable = tableColl;
            
            // Center is below - teleport right side of player
            // float objectRadius = Mathf.Max(coll.bounds.extents.x, coll.bounds.extents.z);
            // float playerRadius = Mathf.Max(playerColl.bounds.extents.x, playerColl.bounds.extents.z);
            // float spawnDistance = objectRadius + playerRadius + 0.5f;
            //
            // Vector3 playerRight = playerColl.transform.right;
            // Vector3 spawnPos = playerColl.transform.position + playerRight * spawnDistance;
            // spawnPos.y = groundObj.transform.position.y + coll.bounds.extents.y; // Place on floor
            // transform.position = spawnPos;
            
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            break;
            
        }
    }
    
    private void CorrectObjectPosition()
    {
        // This function makes sure objects doesn't fall below floor
        // and prevents physic glitches from occuring when released
        // object is inside the player's body!
        
        // Check if object is intersecting with player
        // if (coll.bounds.Intersects(playerColl.bounds))
        // {
        //     // Calculate spawn distance based on object and player size
        //     float objectRadius = Mathf.Max(coll.bounds.extents.x, coll.bounds.extents.z);
        //     float playerRadius = Mathf.Max(playerColl.bounds.extents.x, playerColl.bounds.extents.z);
        //     float spawnDistance = objectRadius + playerRadius + 0.5f; // Add buffer for safety
        //
        //     // Spawn right side of player
        //     Vector3 playerRight = playerColl.transform.right;
        //     Vector3 spawnPos = playerColl.transform.position + playerRight * spawnDistance;
        //     spawnPos.y = floorHeight + coll.bounds.extents.y; // Place on floor
        //     transform.position = spawnPos;
        //
        //     rb.linearVelocity = Vector3.zero;
        //     rb.angularVelocity = Vector3.zero;
        // }
        // Check if object's bottom is below floor (and not in player)
        if (coll.bounds.min.y < floorHeight)
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
        
        // Physics.IgnoreCollision(coll, playerColl, false);
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
        floorHeight = groundObj.transform.position.y;
        // posY_Placement = floorHeight + coll.bounds.extents.y;
        Physics.IgnoreCollision(coll, playerColl, true);
        ignoreObjsTemp = parentObject.IgnoreObjectsTemp;
        rb.solverIterations = 12;
        rb.solverVelocityIterations = 6;
        tables = parentObject.TablesGroup.Tables;
        
        // Debug.Log(parentObject.TablesGroup.Tables[2].name);
        
        if (ignoreObjsTemp == null || ignoreObjsTemp.Count == 0) return;
        foreach (Collider c in ignoreObjsTemp)
        {
            inTriggerDict[c] = false;
        }
    }

    void Update()
    {
        // TODO: Create code that will enable IsTrigger on the edges of the cart for either shelf while
        // holding the syringe. And when you release the object IsTrigger will stay on until OnTriggerExit
        // executes. This is for prevention of collider physics issues when releasing the object during intersection.
        
        // UPDATE: Continue testing TODO functionality from above.
        
        // Debug.Log($"belowTableSurf: {belowTableSurface}");

        if (!grabbable || !rb) return;
        if (!groundObj) return;

        if (transform.position.y < 0 && !grabbable.BeingHeld)
        {
            posY_Placement = floorHeight + coll.bounds.extents.y;
            transform.position = new Vector3(playerColl.transform.position.x, posY_Placement, playerColl.transform.position.z);
        }

        // Debug.Log(grabbable.RemoteGrabDistance);
        
        // started grabbing object
        if (!wasHeldLastFrame && grabbable.BeingHeld)
        {
            // Physics.IgnoreCollision(coll, playerColl, true);
            gameObject.layer = LayerMask.NameToLayer("Grabb");
            
            
            // ignoreObjsTemp.RemoveAll(c => !c);

            if (inTriggerDict.Count > 0)
            {
                Debug.Log("ignoreObjsTemp does exist");
                foreach (Collider colli in ignoreObjsTemp)
                {
                    if (colli == null) continue;
                    // Debug.Log($"{colli.isTrigger} is active");
                    colli.isTrigger = true;
                }
            }

            // if (ignoreObjsTemp == null || ignoreObjsTemp.Count == 0)
            // {
            //     Debug.Log("ignoreObjsTemp is null or not existing");
            // }
            // else
            // {
            //     Debug.Log("ignoreObjsTemp does exist");
            //     foreach (Collider colli in ignoreObjsTemp)
            //     {
            //         if (colli == null) continue;
            //         Debug.Log($"{colli.isTrigger} is active");
            //         colli.isTrigger = true;
            //     }
            // }
        }

        // This is right when the object gets released
        if (wasHeldLastFrame && !grabbable.BeingHeld)
        {
            CheckTableIntersection();
            CorrectObjectPosition();
            
            if (inTriggerDict.Count > 0)
            {
                Debug.Log("ignoreObjsTemp does exist");
                foreach (Collider colli in ignoreObjsTemp)
                {
                    if (colli == null) continue;
                    // Debug.Log($"{colli.isTrigger} is active");
                    if (inTriggerDict[colli]) continue;
                    colli.isTrigger = inTriggerDict[colli];
                }
            }
            
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 10f); // Prevents
            // excessive spinning when object lands on floor

            if (!belowTableSurface)
            {
                StartCoroutine(ReleasePhysics());
            }
        }

        if (belowTableSurface)
        {
            if (!coll.bounds.Intersects(interactedTable.bounds))
            {
                StartCoroutine(ReleasePhysics());
                belowTableSurface = false;
            }
        }
        
        wasHeldLastFrame = grabbable.BeingHeld;
        
        // Debug.Log(string.Join(", ", inTriggerDict.Values));
    }

    // private void OnCollisionEnter(Collision other)
    // {
    //     throw new NotImplementedException();
    // }

    // private void OnCollisionExit(Collision other)
    // {
    //     // if (belowTableSurface)
    //     //
    //     // if (tables.Contains(other.collider))
    //     // {
    //     //     other.isTrigger = false;
    //     // }
    // }

    void OnTriggerEnter(Collider other)
    {
        
        // if (ignoreObjsTemp == null || ignoreObjsTemp.Count == 0) return;
    
        if (ignoreObjsTemp.Contains(other))
        {
            Debug.Log($"{other.gameObject.name} triggered");
            inTriggerDict[other] = true;
        }
    }
    //
    void OnTriggerExit(Collider other)
    {
        // if (ignoreObjsTemp == null || ignoreObjsTemp.Count == 0) return;
        
        // if (gameObject.layer == LayerMask.NameToLayer("Grabb")) return;

        bool isTempIgnoredObj = ignoreObjsTemp.Contains(other);

        if (isTempIgnoredObj)
        {
            inTriggerDict[other] = false;
            // other.isTrigger = inTriggerDict[other];
        }

        if (!grabbable.BeingHeld && isTempIgnoredObj)
        {
            other.isTrigger = false;
        }
        
        // if (tables.Contains(other))
        // {
        //     other.isTrigger = false;
        // }
        
    }
}
