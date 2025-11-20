using UnityEngine;
using UnityEngine.InputSystem;

public class Player2 : MonoBehaviour
{
    // [SerializeField] private Rigidbody playerRigidBody;
    [SerializeField] private CharacterController player;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform groundCheckTransform;
    // [SerializeField] private GameObject coin;
    
    private float horizontalInput;
    private float verticalInput;
    private float xSpeed;
    // private float xCameraRotation;
    // private float yCameraRotation;
    
    // Start is called before the first frame update
    void Start()
    {
        xSpeed = 5f;
        // Debug.Log(playerRigidBody.mass);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;
        verticalInput = context.ReadValue<Vector2>().y;
    }
    
    // public void Looking(InputAction.CallbackContext context)
    // {
    //     cursorInputX = context.ReadValue<Vector2>().x;
    //     cursorInputY = context.ReadValue<Vector2>().y;
    // }

    // Update is called once per frame
    // void Update()
    // {
    //     // isGrounded = player.isGrounded;
    //     // Debug.Log(player.isGrounded);
    // }

    // FixedUpdate is called once every physic update
    void FixedUpdate()
    {
        // Vector3 horizontalMovement = new Vector3(verticalInput, 0, 0);
        // Vector3 verticalMovement = new Vector3(0, 0, horizontalInput);
        Vector3 cameraForward = playerCamera.forward;
        Vector3 cameraRight = playerCamera.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();
        float movementStep = xSpeed * Time.deltaTime;
        Vector3 playerMove = cameraForward * verticalInput + cameraRight * horizontalInput;

        player.Move(playerMove * movementStep);
        // player.Move(horizontalMovement * movementStep);
        // player.Move(verticalMovement * movementStep);
    }
}
