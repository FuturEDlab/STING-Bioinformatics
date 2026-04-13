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
    private bool NoPlayerCollision;
    private Collider coll;
    // private bool isDragging = false;
    private bool isDraggingLeft = false;
    private bool isDraggingRight = false;
    private InputBridge input;
    private BNGPlayerController playerController;
    private DraggableGroup dragGroup;
    // [SerializeField]
    private Rigidbody rb;
    private Transform originalParent;
    private int originalLayer;
    private Vector3 targetPositionLeft;
    private Vector3 targetPositionRight;
    [SerializeField]
    private float springStrength = 1000f;
    [SerializeField]
    private float damping = 100f;
    // [SerializeField]
    // private Rigidbody useRigidBody;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dragGroup = GetComponentInParent<DraggableGroup>();
        input = InputBridge.Instance;
        if (!input) return;
        playerController = input.GetComponentInChildren<BNGPlayerController>();
        if (!playerController) return;
        // if (rb == null)
        // {
        rb = GetComponent<Rigidbody>();
        // }
        originalParent = transform.parent;
        coll = GetComponent<Collider>();
        originalLayer = gameObject.layer;
    }
    
    // Update is called once per frame
    void Update()
    {
        // if (NoPlayerCollision)
        // {
        //     Physics.IgnoreCollision(coll, dragGroup.PlayerCollider, false);
        // }
        
        if (!touchingLeft && !touchingRight) return;

        // if (Input.GetKeyDown(KeyCode.B))
        if (OnlyLeftGripPressed())
        {
            isDraggingLeft = true;
            // Physics.IgnoreCollision(coll, dragGroup.PlayerCollider, true);
        }
        
        // if (Input.GetKeyUp(KeyCode.B))
        if (input.LeftGrip < 0.1f)
        {
            isDraggingLeft = false;
            // StartCoroutine(ReleasePhysics());
        }
        
        if (OnlyRightGripPressed())
        {
            isDraggingRight = true;
            // Physics.IgnoreCollision(coll, dragGroup.PlayerCollider, true);
        }
        
        // if (Input.GetKeyUp(KeyCode.B))
        if (input.RightGrip < 0.1f)
        {
            isDraggingRight = false;
            // StartCoroutine(ReleasePhysics());
        }
        
        if (isDraggingLeft)
        {
            Vector3 handPos = dragGroup.LeftHand.transform.position;

            // Keep object on floor plane
            targetPositionLeft = new Vector3(handPos.x, transform.position.y, handPos.z);
        }
        
        if (isDraggingRight)
        {
            Vector3 handPos = dragGroup.RightHand.transform.position;

            // Keep object on floor plane
            targetPositionRight = new Vector3(handPos.x, transform.position.y, handPos.z);
        }

    }
    
    void FixedUpdate()
    {
        if (!isDraggingLeft && !isDraggingRight) return;

        Vector3 force;

        // TODO: upon request, add a condition for when both are dragging (Left and Right)
        if (isDraggingLeft)
        {
            force = (targetPositionLeft - rb.position) * springStrength;
            rb.AddForce(force - rb.linearVelocity * damping, ForceMode.Acceleration);
        }
        else if (isDraggingRight)
        {
            force = (targetPositionRight - rb.position) * springStrength;
            rb.AddForce(force - rb.linearVelocity * damping, ForceMode.Acceleration);
        }

    }
    
    private IEnumerator ReleasePhysics()
    {
        // Keep collisions ignored for one physics step
        yield return new WaitForFixedUpdate();

        // if (NoPlayerCollision)
        // {
        Physics.IgnoreCollision(coll, dragGroup.PlayerCollider, false);
            // gameObject.layer = originalLayer;
        // }
    }

    // private void OnCollisionEnter(Collision other)
    // {
    //     if (other.transform.CompareTag("Player"))
    //     {
    //         NoPlayerCollision = false;
    //         Debug.Log($"Player collided with {transform.name}");
    //     }
    // }
    //
    // private void OnCollisionExit(Collision other)
    // {
    //     if (other.transform.CompareTag("Player"))
    //     {
    //         NoPlayerCollision = true;
    //         Debug.Log($"Player stopped colliding with {transform.name}");
    //     }
    // }

    void OnTriggerEnter(Collider other)
    {
        // Tags LeftHand and RightHand are associated with the Grabber child objects
        // from LeftController object and RightController object (XR Rig Full Body,
        // the parent object, contains these 2 Controller objects)
        
        if (other.CompareTag("LeftHand"))
        {
            touchingLeft = true;
        }
        else if (other.CompareTag("RightHand"))
        {
            touchingRight = true;
        }
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
        // return input.RightGrip > 0.9f;
        return input.RightGripDown;
    }
    
}
