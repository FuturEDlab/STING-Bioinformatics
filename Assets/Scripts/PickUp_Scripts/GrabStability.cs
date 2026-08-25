using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class GrabStability : MonoBehaviour
{
    [Header("Release feel")]
    [Tooltip("How much of the hand's speed the object keeps when you let go. 1 is the rig's untouched throw. Nothing in this sim is meant to be thrown, so a release should read as putting something down - and tracked hands report a speed spike on the frame the grab ends, which is what makes a gently released bottle fly.")]
    [Range(0f, 1f)]
    [SerializeField] private float releaseSpeedScale = 0.35f;

    [Tooltip("Ceiling on release speed in metres per second, applied after the scale above.")]
    [SerializeField] private float maxReleaseSpeed = 1.25f;

    [Tooltip("How much of the hand's spin the object keeps. Kept much lower than the speed: angular noise from hand tracking is what sets a bottle rolling in the first place.")]
    [Range(0f, 1f)]
    [SerializeField] private float releaseSpinScale = 0.12f;

    [Tooltip("Ceiling on release spin in radians per second.")]
    [SerializeField] private float maxReleaseSpin = 1.5f;

    [Tooltip("How long after a release those ceilings keep being enforced. Insurance against anything that adds velocity a physics step later.")]
    [SerializeField] private float releaseSettleWindow = 0.25f;

    [Header("Coming to rest")]
    [Tooltip("Angular damping while the object is loose. PhysX has no rolling friction, so a rounded bottle on a flat table rolls until it hits something unless this is well above the Rigidbody's own 0.05.")]
    [SerializeField] private float looseAngularDamping = 5f;

    [Tooltip("Linear damping while the object is loose.")]
    [SerializeField] private float looseLinearDamping = 1f;

    [Tooltip("Below this speed (m/s) AND this spin (rad/s) the object counts as settled.")]
    [SerializeField] private float restSpeedThreshold = 0.06f;
    [SerializeField] private float restSpinThreshold = 0.5f;

    [Tooltip("How long it has to stay under both thresholds before its motion is zeroed outright. Kills the last centimetre of creep that damping alone leaves behind.")]
    [SerializeField] private float restDelay = 0.35f;

    private Rigidbody rb;
    private GrabHandle grab;
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
    private float heldLinearDamping;
    private float heldAngularDamping;
    private float settleTimer;
    private float restTimer;
    
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
        // GrabHandle reports "held" for a BNG Grabbable, an XRI XRGrabInteractable, or the
        // desktop mouse pointer, so the settle-down physics behave the same on either rig.
        grab = new GrabHandle(this, searchRelatives: false);
        coll = GetComponent<Collider>();
        parentObject = GetComponentInParent<PickUpGroup>();
        originalLayer = gameObject.layer;
        playerColl = parentObject.PlayerCollider;
        groundObj = parentObject.Ground;
        
        floorHeight = groundObj.transform.position.y;

        // An empty Player Collider slot used to mean a NullReferenceException here and no
        // stability behaviour at all. PickUpGroup now falls back to the rig's capsule, but
        // stay defensive: a prop that cannot ignore the player is still worth settling.
        if (playerColl != null)
            Physics.IgnoreCollision(coll, playerColl, true);
        else
            Debug.LogWarning($"[GrabStability] '{name}' has no player collider to ignore — held props will push the player around. Set Player Collider on the PickUpGroup, or add a rig with a CharacterController.", this);

        ignoreObjsTemp = parentObject.IgnoreObjectsTemp;
        rb.solverIterations = 12;
        rb.solverVelocityIterations = 6;
        heldLinearDamping = rb.linearDamping;
        heldAngularDamping = rb.angularDamping;
        tables = parentObject.TablesGroup.Tables;
        
        if (ignoreObjsTemp == null || ignoreObjsTemp.Count == 0) return;
        foreach (Collider c in ignoreObjsTemp)
        {
            inTriggerDict[c] = false;
        }
    }

    void Update()
    {
        if (grab == null || !grab.Exists || !rb) return;
        if (!groundObj) return;

        if (transform.position.y < 0 && !grab.IsHeld)
        {
            posY_Placement = floorHeight + coll.bounds.extents.y;
            // Fell through the world: put it back at the player's feet. Without a player
            // collider, put it back where it is rather than at the world origin.
            Vector3 feet = playerColl != null ? playerColl.transform.position : transform.position;
            transform.position = new Vector3(feet.x, posY_Placement, feet.z);
            gameObject.layer = originalLayer;
        }
        
        // started grabbing object
        if (!wasHeldLastFrame && grab.IsHeld)
        {
            gameObject.layer = LayerMask.NameToLayer("Grabb");
            ManageTriggers(true);

            // While held the rig drives the body through physics, so give it back the damping
            // the Rigidbody was authored with and drop any settling still in progress.
            rb.linearDamping = heldLinearDamping;
            rb.angularDamping = heldAngularDamping;
            settleTimer = 0f;
            restTimer = 0f;
        }

        // This is right when the object gets released
        if (wasHeldLastFrame && !grab.IsHeld)
        {
            CheckTableIntersection();
            CorrectObjectPosition();
            ManageTriggers(false);
            TameRelease();

            IntersectedBelow_OnRelease();
        }

        IntersectedBelow_PastRelease();
        wasHeldLastFrame = grab.IsHeld;
    }

    /// <summary>
    /// Turn the rig's throw into a place-down, once, on the frame the object is let go.
    ///
    /// Both rigs apply the tracked hand velocity in the same synchronous call that ends the
    /// grab — BNG in Grabbable.DropItem, XRI as the interactable detaches — so by the time
    /// Update() here sees the held state go false the throw is already on the Rigidbody, and
    /// this is scaling the real number rather than one that is about to be overwritten.
    /// </summary>
    private void TameRelease()
    {
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity * releaseSpeedScale, maxReleaseSpeed);
        rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity * releaseSpinScale, maxReleaseSpin);

        // Loose on a table with no rolling friction, a bottle needs damping of its own or it
        // rolls until it falls off the edge.
        rb.linearDamping = looseLinearDamping;
        rb.angularDamping = looseAngularDamping;

        settleTimer = releaseSettleWindow;
        restTimer = 0f;
    }

    private void FixedUpdate()
    {
        if (!rb || grab == null || !grab.Exists || grab.IsHeld)
            return;

        // The ceilings are re-applied (never re-scaled - that would shrink the velocity to
        // nothing over the window) so anything that adds motion a step or two after the
        // release still lands inside the same limits.
        if (settleTimer > 0f)
        {
            settleTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxReleaseSpeed);
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, maxReleaseSpin);
        }

        bool crawling = rb.linearVelocity.magnitude < restSpeedThreshold &&
                        rb.angularVelocity.magnitude < restSpinThreshold;

        if (!crawling)
        {
            restTimer = 0f;
            return;
        }

        restTimer += Time.fixedDeltaTime;

        if (restTimer < restDelay)
            return;

        // Held under both thresholds long enough to be sitting still in all but name.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
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

        if (grab != null && !grab.IsHeld && isTempIgnoredObj)
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
