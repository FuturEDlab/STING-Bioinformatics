using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    // [SerializeField] private Player2 playerrr;
    // [SerializeField] private Transform groundCheckTransform;
    
    private float xSpeed;
    private float xCameraRotation;
    private float yCameraRotation;
    private float cursorInputX;
    private float cursorInputY;
    private float sensivity = 70f;
    
    // Start is called before the first frame update
    void Start()
    {
        xSpeed = 2f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void Looking(InputAction.CallbackContext context)
    {
        cursorInputX = context.ReadValue<Vector2>().x;
        cursorInputY = context.ReadValue<Vector2>().y;
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log(cursorInputX);
        float mouseX = cursorInputX * sensivity * Time.deltaTime;
        float mouseY = cursorInputY * sensivity * Time.deltaTime;
        
        playerBody.Rotate(Vector3.up * mouseX);
        
        xCameraRotation -= mouseY;
        xCameraRotation = Mathf.Clamp(xCameraRotation, -80f, 80f);
        
        transform.localRotation = Quaternion.Euler(xCameraRotation, 0f, 0f);
    }

    // FixedUpdate is called once every physic update
    // void FixedUpdate()
    // {
    //     Vector3 horizontalMovement = new Vector3(cursorInputX, 0, 0);
    //     Vector3 verticalMovement = new Vector3(cursorInputY, 0, 0);
    //     float movementStep = xSpeed * Time.deltaTime;
    //
    //     // player.Move(horizontalMovement * movementStep);
    //     // player.
    // }
}
