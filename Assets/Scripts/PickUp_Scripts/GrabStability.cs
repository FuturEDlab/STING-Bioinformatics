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
    private HashSet<Collider> tables;
    private bool belowTableSurface;
    private Collider interactedTable;
    
    private void CheckTableIntersection()
    {
        // This function determines whether an object should drop below table
        // or snap back on top of table.

        if (tables == null) return;
        
        foreach (Collider tableColl in tables)
        {
            // When released object isn't intersecting with any tables in scene
            if (!coll.bounds.Intersects(tableColl.bounds)) continue;
            
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
            
            belowTableSurface = true;
            interactedTable = tableColl;
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
        Physics.IgnoreCollision(coll, playerColl, true);
        ignoreObjsTemp = parentObject.IgnoreObjectsTemp;
        rb.solverIterations = 12;
        rb.solverVelocityIterations = 6;
        tables = parentObject.TablesGroup.Tables;
        
        if (ignoreObjsTemp == null || ignoreObjsTemp.Count == 0) return;
        foreach (Collider c in ignoreObjsTemp)
        {
            inTriggerDict[c] = false;
        }
    }

    void Update()
    {
        if (!grabbable || !rb) return;
        if (!groundObj) return;

        if (transform.position.y < 0 && !grabbable.BeingHeld)
        {
            posY_Placement = floorHeight + coll.bounds.extents.y;
            transform.position = new Vector3(playerColl.transform.position.x, posY_Placement, playerColl.transform.position.z);
            gameObject.layer = originalLayer;
        }
        
        // started grabbing object
        if (!wasHeldLastFrame && grabbable.BeingHeld)
        {
            gameObject.layer = LayerMask.NameToLayer("Grabb");
            ManageTriggers(true);
        }

        // This is right when the object gets released
        if (wasHeldLastFrame && !grabbable.BeingHeld)
        {
            CheckTableIntersection();
            CorrectObjectPosition();
            ManageTriggers(false);
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 10f); // Prevents
            // excessive spinning when object lands on floor

            IntersectedBelow_OnRelease();
        }

        IntersectedBelow_PastRelease();
        wasHeldLastFrame = grabbable.BeingHeld;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("CabinetFloorLeft") || other.name.Contains("CabinetFloorRight"))
        {
            gameObject.layer = LayerMask.NameToLayer("Grabb");
        }
        
        if (ignoreObjsTemp.Contains(other))
        {
            inTriggerDict[other] = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        bool isTempIgnoredObj = ignoreObjsTemp.Contains(other);

        if (isTempIgnoredObj)
        {
            inTriggerDict[other] = false;
        }

        if (!grabbable.BeingHeld && isTempIgnoredObj)
        {
            other.isTrigger = false;
        }
    }

    void ManageTriggers(bool grabInitiated)
    {
        if (inTriggerDict.Count <= 0) return;

        foreach (Collider colli in ignoreObjsTemp)
        {
            if (colli == null) continue;

            if (grabInitiated)
            {
                colli.isTrigger = true;
                continue;
            }
            
            if (inTriggerDict[colli]) continue;
            colli.isTrigger = inTriggerDict[colli];
        }
    }

    void IntersectedBelow_OnRelease()
    {
        if (!belowTableSurface)
        {
            StartCoroutine(ReleasePhysics());
        }
    }
    
    void IntersectedBelow_PastRelease()
    {
        if (!belowTableSurface) return;
        
        if (!coll.bounds.Intersects(interactedTable.bounds))
        {
            StartCoroutine(ReleasePhysics());
            belowTableSurface = false;
        }
    }
}
