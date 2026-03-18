using System;
using UnityEngine;
using UnityEngine.Events;
using BNG;
using Unity.VisualScripting;
using System.Collections;

public class Draggable : MonoBehaviour
{
    // todo: Finish implementing code for this script
    // [SerializeField] private UnityEvent onDrag;

    private bool touchingRight;
    private bool touchingLeft;

    private Collider coll;

    // private Collider playerColl;
    // private bool isDragging = false;
    private bool isDraggingLeft = false;
    private bool isDraggingRight = false;
    private InputBridge input;
    private BNGPlayerController playerController;
    private DraggableGroup dragGroup;
    private Vector3 lastPosition;
    private Vector3 lastPositionLeft;
    private Vector3 lastPositionRight;
    private Vector3 currentPosition;
    private Vector3 currentPositionLeft;
    private Vector3 currentPositionRight;
    // private Vector3 delta;
    private Vector3 deltaLeft;
    private Vector3 deltaRight;
    private Rigidbody rb;
    private Transform originalParent;
    private Quaternion lastRotation;
    private CharacterController characterController;
    private Vector3 smoothedDelta;
    private Vector3 cachedLeftHandPosition;
    // private JointHelper jntHelper;
    private int originalLayer;
    private Vector3 targetPosition;
    private Vector3 lastMonitorPosition;
    [SerializeField]
    private float springStrength = 1000f;
    [SerializeField]
    private float damping = 100f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
   
         
        dragGroup = GetComponentInParent<DraggableGroup>();
        input = InputBridge.Instance;
        if (!input) return;
        playerController = input.GetComponentInChildren<BNGPlayerController>();
        if (!playerController) return;
        // lastPosition = playerController.transform.position;
        lastPositionLeft = dragGroup.LeftHand.transform.position;
        lastPositionRight = dragGroup.RightHand.transform.position;
        rb = GetComponent<Rigidbody>();
        // if (!rb) return;
        originalParent = transform.parent;
        coll = GetComponent<Collider>();
        lastMonitorPosition = transform.position;
        originalLayer = gameObject.layer;
        // jntHelper = GetComponent<JointHelper>();
        // lastRotation = playerController.transform.rotation;
        // characterController = playerController.GetComponent<CharacterController>();
        // if (characterController)
        // {
        //     Debug.Log($"gottt character controller");
        // }
    }

    // void OnControllerColliderHit(ControllerColliderHit hit)
    // {
    //     Rigidbody rigidB = hit.collider.attachedRigidbody;
    //     
    //     if (hit.collider.CompareTag("TV"))
    //     {
    //         Debug.Log($"hit player info: {hit.moveDirection}");
    //         if (rigidB != null)
    //         {
    //             Vector3 forceDir = hit.gameObject.transform.position - transform.position;
    //             forceDir.y = 0f;
    //             forceDir.Normalize();
    //             
    //             rigidB.AddForceAtPosition(forceDir * 1, transform.position, ForceMode.Impulse);
    //         }
    //     }
    // }


    // void Update()
    // {
    //     
    //     // Vector3 playerDelta = dragGroup.LeftHand.transform.position - lastPositionLeft;
    //     // Vector3 monitorDelta = transform.position - lastMonitorPosition;
    //     //
    //     // transform.position += new Vector3(playerDelta.x, 0, playerDelta.z);
    //     //
    //     // Debug.Log($"Player delta: {playerDelta} | Monitor delta: {monitorDelta}");
    //     //
    //     // lastPositionLeft = dragGroup.LeftHand.transform.position;
    //     // lastMonitorPosition = transform.position;
    //     
    //     // currentPosition = dragGroup.LeftHand.transform.position;
    //     // deltaLeft = currentPosition - lastPositionLeft;
    //     // lastPositionLeft = currentPosition;
    //
    //     if (!touchingLeft && !touchingRight) return;
    //     
    //     currentPosition = dragGroup.LeftHand.transform.position;
    //     deltaLeft = currentPosition - lastPositionLeft;
    //     lastPositionLeft = currentPosition;
    //
    //     if (Input.GetKey(KeyCode.B))
    //     {
    //         rb.isKinematic = true;
    //         Debug.Log($"delta: {deltaLeft} | magnitude: {deltaLeft.magnitude} | monitor pos: {transform.position}");
    //         if (deltaLeft.magnitude > 0.001f)
    //         {
    //             transform.position += new Vector3(deltaLeft.x, 0, deltaLeft.z);
    //         }
    //     }
    //
    //     if (Input.GetKeyUp(KeyCode.B))
    //     {
    //         rb.isKinematic = false;
    //     }
    // }
    //
    // void OnTriggerEnter(Collider other)
    // {
    //     // Tags LeftHand and RightHand are associated with the Grabber child objects
    //     // from LeftController object and RightController object (XR Rig Full Body,
    //     // the parent object, contains these 2 Controller objects)
    //     
    //     if (other.CompareTag("LeftHand"))
    //     {
    //         touchingLeft = true;
    //         // rb.isKinematic = true;
    //         // transform.SetParent(playerController.transform);
    //     }
    //     else if (other.CompareTag("RightHand"))
    //     {
    //         touchingRight = true;
    //         // rb.isKinematic = true;
    //         // transform.SetParent(playerController.transform);
    //     }
    //
    //     // if (!touchingLeft && !touchingRight) return;
    //
    //     // IsDragButtonPressed();
    //     
    //     // touchingLeft = false;
    //     // touchingRight = false;
    // }
    //
    // void OnTriggerExit(Collider other)
    // {
    //     // Tags LeftHand and RightHand are associated with the Grabber child objects
    //     // from LeftController object and RightController object (XR Rig Full Body,
    //     // the parent object, contains these 2 controller objects)
    //     
    //     if (other.CompareTag("LeftHand"))
    //     {
    //         touchingLeft = false;
    //         // rb.isKinematic = false;
    //         // transform.SetParent(originalParent);
    //     }
    //     else if (other.CompareTag("RightHand"))
    //     {
    //         touchingRight = false;
    //         // rb.isKinematic = false;
    //         // transform.SetParent(originalParent);
    //     }
    // }

    
    // Update is called once per frame
    void Update()
    {
        // Vector3 playerDelta = dragGroup.LeftHand.transform.position - lastPositionLeft;
        // Vector3 monitorDelta = transform.position - lastMonitorPosition;
        //
        // transform.position += new Vector3(playerDelta.x, 0, playerDelta.z);
        //
        // Debug.Log($"Player delta: {playerDelta} | Monitor delta: {monitorDelta}");
        //
        // lastPositionLeft = dragGroup.LeftHand.transform.position;
        // lastMonitorPosition = transform.position;
        
        if (!touchingLeft && !touchingRight) return;
        
        // Vector3 playerDelta = dragGroup.LeftHand.transform.position - lastPositionLeft;
        // Vector3 monitorDelta = rb.position - lastMonitorPosition;
        //
        // Debug.Log($"Player delta: {playerDelta} | Monitor delta: {monitorDelta}");
        //
        // lastPositionLeft = dragGroup.LeftHand.transform.position;
        // lastMonitorPosition = rb.position;
        
        // cachedLeftHandPosition = dragGroup.LeftHand.transform.position;

        // if (touchingLeft)
        // {
        //     currentPositionLeft = dragGroup.LeftHand.transform.position;
        //     deltaLeft = currentPositionLeft - lastPositionLeft;
        // }
        // if (touchingRight)
        // {
        //     currentPositionRight = dragGroup.RightHand.transform.position;
        //     deltaRight = currentPositionRight - lastPositionRight;
        // }
        
        // currentPosition = playerController.transform.position;
        // delta = currentPosition - lastPosition;
        // Vector3 playerVelocity = characterController.velocity;
        
        // currentPosition = playerController.transform.position;
        // delta = currentPosition - lastPosition;

        // if (Input.GetKeyDown(KeyCode.B))
        if (OnlyLeftGripPressed())
        {
            // jntHelper.enabled = true;
            // if (touchingLeft)
            // {
                // jntHelper.enabled = true;
            isDraggingLeft = true;
            Physics.IgnoreCollision(coll, dragGroup.PlayerCollider, true);
            // gameObject.layer = LayerMask.NameToLayer("Grabb");
                // lastPositionLeft = cachedLeftHandPosition;
                // smoothedDelta = Vector3.zero;
                // transform.SetParent(dragGroup.LeftHand.transform);
            // }
            // rb.MovePosition(rb.position + new Vector3(delta.x, 0, delta.z));
        }

        // if (isDraggingLeft)
        // {
        //     // if (deltaLeft.magnitude > 0.001f)
        //     // {
        //         // Debug.Log("should be moving monitor");
        //     transform.position += new Vector3(deltaLeft.x, 0, deltaLeft.z);
        //     // }
        // }
        
        // if (isDragging)
        // {
        //     if (Quaternion.Angle(playerController.transform.rotation, lastRotation) > 0.1f)
        //     {
        //         Debug.Log("player rotated while grabbing");
        //         transform.SetParent(originalParent);
        //         isDragging = false;
        //     }
        // }
        
        // if (Input.GetKeyUp(KeyCode.B))
        if (input.LeftGrip < 0.1f)
        {
            // transform.SetParent(originalParent);
            isDraggingLeft = false;
            StartCoroutine(ReleasePhysics());
            // Physics.IgnoreCollision(coll, dragGroup.PlayerCollider, false);
            // jntHelper.enabled = false;
            // isDragging = false;
            // rb.isKinematic = false;
        }
        
        if (isDraggingLeft)
        {
            Vector3 handPos = dragGroup.LeftHand.transform.position;

            // Keep object on floor plane
            targetPosition = new Vector3(handPos.x, transform.position.y, handPos.z);
        }

        // lastPosition = currentPosition;
        // lastPositionLeft = currentPositionLeft;
    }
    
    void FixedUpdate()
    {
        if (!isDraggingLeft) return;
        
        Vector3 force = (targetPosition - rb.position) * springStrength;

        rb.AddForce(force - rb.linearVelocity * damping, ForceMode.Acceleration);
        
        // currentPositionLeft = cachedLeftHandPosition;
        // deltaLeft = currentPositionLeft - lastPositionLeft;

        // if (deltaLeft.magnitude < 0.001f)
        // {
        //     deltaLeft = Vector3.zero;
        // }
        //
        // smoothedDelta = Vector3.Lerp(smoothedDelta, deltaLeft, 0.5f);
        //
        // rb.MovePosition(rb.position + new Vector3(smoothedDelta.x, 0, smoothedDelta.z));
        //
        // lastPositionLeft = currentPositionLeft;

        // currentPositionLeft = dragGroup.LeftHand.transform.position;
        // deltaLeft = currentPositionLeft - lastPositionLeft;
        //
        // rb.MovePosition(rb.position + new Vector3(deltaLeft.x, 0, deltaLeft.z));
        // // transform.position += new Vector3(deltaLeft.x, 0, deltaLeft.z);
        //
        // lastPositionLeft = currentPositionLeft;
    }
    
    private IEnumerator ReleasePhysics()
    {
        // Keep collisions ignored for one physics step
        yield return new WaitForFixedUpdate();
        
        Physics.IgnoreCollision(coll, dragGroup.PlayerCollider, false);
        gameObject.layer = originalLayer;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Tags LeftHand and RightHand are associated with the Grabber child objects
        // from LeftController object and RightController object (XR Rig Full Body,
        // the parent object, contains these 2 Controller objects)
        
        if (other.CompareTag("LeftHand"))
        {
            touchingLeft = true;
            // transform.SetParent(playerController.transform);
        }
        else if (other.CompareTag("RightHand"))
        {
            touchingRight = true;
            // transform.SetParent(playerController.transform);
        }

        // if (!touchingLeft && !touchingRight) return;

        // IsDragButtonPressed();
        
        // touchingLeft = false;
        // touchingRight = false;
    }

    void OnTriggerExit(Collider other)
    {
        // Tags LeftHand and RightHand are associated with the Grabber child objects
        // from LeftController object and RightController object (XR Rig Full Body,
        // the parent object, contains these 2 controller objects)
        
        if (other.CompareTag("LeftHand"))
        {
            touchingLeft = false;
            transform.SetParent(originalParent);
        }
        else if (other.CompareTag("RightHand"))
        {
            touchingRight = false;
            transform.SetParent(originalParent);
        }
    }
    
    private bool OnlyLeftGripPressed()
    {
        if (!touchingLeft) return false;

        // return (input.LeftGrip > 0.9f && input.RightGrip < 0.1f);
        // return input.LeftGrip > 0.9f;
        return input.LeftGripDown;

        // bool isPressed = false;
        //
        // if (input.LeftGripDown && input.RightGrip < 0.1f)
        // {
        //     Debug.Log($"left hand touching while left button down");
        //     isPressed = true;
        // }
        //
        // return isPressed;
    }
    
    private bool OnlyRightGripPressed()
    {
        if (!touchingRight) return false;

        // return (input.RightGrip > 0.9f && input.LeftGrip < 0.1f);
        return input.RightGrip > 0.9f;
    }

    void IsDragButtonPressed()
    {
        if (!InputBridge.Instance) return;
        
        InputBridge input = InputBridge.Instance;

        while (input.LeftGripDown && input.RightGrip < 0.1f)
        {
            if (touchingLeft)
            {
                Debug.Log($"left hand touching while left button down");
            }
        }
        
        while (input.RightGripDown && input.LeftGrip < 0.1f)
        {
            if (touchingRight)
            {
                Debug.Log($"left hand touching while left button down");
            }
        }
        
    }
    
}
