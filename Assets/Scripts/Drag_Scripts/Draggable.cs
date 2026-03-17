using UnityEngine;
using UnityEngine.Events;
using BNG;

public class Draggable : MonoBehaviour
{
    // todo: Finish implementing code for this script
    // [SerializeField] private UnityEvent onDrag;

    private bool touchingRight;
    private bool touchingLeft;
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
        originalParent = transform.parent;
        // lastRotation = playerController.transform.rotation;
        // characterController = playerController.GetComponent<CharacterController>();
        // if (characterController)
        // {
        //     Debug.Log($"gottt character controller");
        // }
    }

    // Update is called once per frame
    void Update()
    {
        // transform.SetParent(playerController.transform);
        
        // currentPosition = playerController.transform.position;
        // delta = currentPosition - lastPosition;
        
        // if (delta.magnitude > 0.001f)
        // {
        //     Debug.Log("should be moving monitor");
        //     // rb.MovePosition(rb.position + new Vector3(delta.x, 0, delta.z));
        //     transform.position += new Vector3(delta.x, 0, delta.z);
        // }
        //
        // lastPosition = currentPosition;

        if (!touchingRight && !touchingRight) return;

        if (touchingLeft)
        {
            currentPositionLeft = dragGroup.LeftHand.transform.position;
            deltaLeft = currentPositionLeft - lastPositionLeft;
        }
        if (touchingRight)
        {
            currentPositionRight = dragGroup.RightHand.transform.position;
            deltaRight = currentPositionRight - lastPositionRight;
        }
        
        // currentPosition = playerController.transform.position;
        // delta = currentPosition - lastPosition;
        //
        // if (delta.magnitude >= 0.001f)
        // {
        //     Debug.Log("should be moving monitor");
        //     rb.MovePosition(rb.position + new Vector3(delta.x, 0, delta.z));
        // }
        
        Vector3 playerVelocity = characterController.velocity;
        
        // currentPosition = playerController.transform.position;
        // delta = currentPosition - lastPosition;

        if (Input.GetKeyDown(KeyCode.B))
        // if (OnlyLeftGripPressed())
        {
            // Debug.Log($"velocity: {playerVelocity}");
            // Debug.Log($"magnitude: {playerVelocity.magnitude}");
            // if (delta.magnitude > 0.001f)
            // {
            //     Debug.Log("should be moving monitor"); 

            if (touchingLeft)
            {
                isDraggingLeft = true;
                // transform.SetParent(dragGroup.LeftHand.transform);
            }
            // if (touchingRight)
            // {
            //     // isDraggingRight = true;
            //     // transform.SetParent(dragGroup.RightHand.transform);
            // }
            //
            // lastRotation = playerController.transform.rotation;
            // isDragging = true;
                // transform.position += new Vector3(delta.x, 0, delta.z);
                // rb.MovePosition(rb.position + new Vector3(delta.x, 0, delta.z));
            
            // if (Quaternion.Angle(playerController.transform.rotation, lastRotation) > 0.1f) {
            //     Debug.Log($"player rotated while grabbing");
            //     transform.SetParent(originalParent);
            //     lastRotation = playerController.transform.rotation;
            // }
            // Debug.Log(playerController.transform);
            // if (Quaternion.Angle(playerController.transform.rotation, lastRotation) > 0.1f)
            // if (playerController.transform.position == lastPosition)
            // { 
            //     // Debug.Log($"player only rotating");
            //     Debug.Log($"player position moving");
            //     // lastRotation = playerController.transform.rotation;
            //     lastPosition = playerController.transform.position;
            //     // transform.SetParent(originalParent);
            //     transform.SetParent(originalParent);
            // }
            // else
            // {
            //     transform.SetParent(originalParent);
            //     // transform.SetParent(playerController.transform);
            // }
            // Vector3 currentRotation = transform.eulerAngles;
            // transform.eulerAngles = new Vector3(0f, 0f, 0f);
            // transform.rotation.y = Quaternion.identity;

            // isDragging = true;
            // lastPosition = playerController.transform.position;
            // delta = playerController.transform.position - lastPosition;
            // rb.MovePosition(rb.position + new Vector3(delta.x, 0, delta.z));
            // lastPosition = playerController.transform.position;

            // if (touchingLeft)
            // {
            //     transform.SetParent(playerController.transform);
            // }
            // else
            // {
            //     transform.SetParent(originalParent);
            // }
        }

        if (isDraggingLeft)
        {
            if (deltaLeft.magnitude >= 0.001f)
            {
                // Debug.Log("should be moving monitor");
                transform.position += new Vector3(deltaLeft.x, 0, deltaLeft.z);
            }
        }
        
        // if (isDragging)
        // {
        //     if (Quaternion.Angle(playerController.transform.rotation, lastRotation) > 0.1f)
        //     {
        //         Debug.Log("player rotated while grabbing");
        //         transform.SetParent(originalParent);
        //         isDragging = false;
        //     }
        // }
        
        if (Input.GetKeyUp(KeyCode.B))
        // if (input.LeftGrip < 0.1f && transform.parent != originalParent)
        {
            // transform.SetParent(originalParent);
            isDraggingLeft = false;
            // isDragging = false;
            // rb.isKinematic = false;
        }

        lastPosition = currentPosition;
        // lastRotation = playerController.transform.rotation;

        // lastRotation = playerController.transform.rotation;

        // if (Input.GetKey(KeyCode.X))
        // if (OnlyRightGripPressed())
        // {
        //     // Debug.Log("right hand touching while right button down");
        //     transform.SetParent(playerController.transform);
        // }
        //
        // // if (input.RightGrip < 0.1f && transform.parent != originalParent)
        // if (Input.GetKeyUp(KeyCode.X))
        // {
        //     transform.SetParent(originalParent);
        // }
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
        return input.LeftGrip > 0.9f;

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
