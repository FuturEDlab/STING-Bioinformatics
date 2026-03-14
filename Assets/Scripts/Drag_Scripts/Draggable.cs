using UnityEngine;
using UnityEngine.Events;
using BNG;

public class Draggable : MonoBehaviour
{
    // todo: Finish implementing code for this script
    // [SerializeField] private UnityEvent onDrag;

    private bool touchingRight;
    private bool touchingLeft;
    private InputBridge input;
    private BNGPlayerController playerController;
    private DraggableGroup dragGroup;
    private Vector3 lastPosition;
    private Vector3 currentPosition;
    private Vector3 delta;
    private Rigidbody rb;
    private Transform originalParent;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dragGroup = GetComponentInParent<DraggableGroup>();
        input = InputBridge.Instance;
        if (!input) return;
        playerController = input.GetComponentInChildren<BNGPlayerController>();
        if (!playerController) return;
        lastPosition = playerController.transform.position;
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        // if (!playerController) return;
        if (!touchingLeft && !touchingRight) return;
        
        // currentPosition = playerController.transform.position;
        // delta = currentPosition - lastPosition;

        if (Input.GetKey(KeyCode.B))
        // if (OnlyLeftGripPressed())
        {
            // currentPosition = playerController.transform.position;
            // delta = currentPosition - lastPosition;
            // Debug.Log(delta);
            // Debug.Log("left hand touching while left button down");
            // Debug.Log($"raw Velocity - {dragGroup.PlayerRb.linearVelocity}");
            // playerController.;
            
            transform.SetParent(playerController.transform);
            
            // if (delta.z >= 0.01f || delta.z <= -0.01f)
            // {
            //     // rb.MovePosition(rb.position + new Vector3(0, 0, delta.z));
            //     transform.position += new Vector3(0, 0, delta.z) * Time.deltaTime;
            // }
            // if (delta.x >= 0.01f || delta.x <= -0.01f)
            // {
            //     // rb.MovePosition(rb.position + new Vector3(0, 0, delta.x));
            //     transform.position += new Vector3(0, 0, delta.x) * Time.deltaTime;
            // }
            
            // if (delta.z >= 0.01f)
            // {
            //     Debug.Log("Moving right");
            // }
            // if (delta.z <= -0.01f)
            // {
            //     Debug.Log("Moving left");
            // }
            // if (delta.x >= 0.01f)
            // {
            //     Debug.Log("Moving forward");
            // }
            // if (delta.x <= -0.01f)
            // {
            //     Debug.Log("Moving backward");
            // }

            
            // if (dragGroup.PlayerRb.linearVelocity.z > 0.1f)
            // {
            //     Debug.Log("Moving Forward");
            // }
            // else if (dragGroup.PlayerRb.linearVelocity.z < -0.1f)
            // {
            //     Debug.Log("Moving Backward");
            // }
        }
        
        if (Input.GetKeyUp(KeyCode.B))
        {
            transform.SetParent(originalParent);
        }

        if (Input.GetKey(KeyCode.X))
        // if (OnlyRightGripPressed())
        {
            // Debug.Log("right hand touching while right button down");
            transform.SetParent(playerController.transform);
        }
        
        if (Input.GetKeyUp(KeyCode.X))
        {
            transform.SetParent(originalParent);
        }
        
        // transform.SetParent(originalParent);

        // lastPosition = currentPosition;
    }
    
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
        }
        else if (other.CompareTag("RightHand"))
        {
            touchingRight = false;
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
