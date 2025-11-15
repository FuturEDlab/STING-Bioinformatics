// using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private Transform groundCheckTransform;
    // [SerializeField] private GameObject coin;
    
    private float horizontalInput;
    private Rigidbody rigidBodyComponent;
    private bool isGrounded;
    
    // Start is called before the first frame update
    void Start()
    {
        rigidBodyComponent = GetComponent<Rigidbody>();
        // coins = GameObject.FindGameObjectsWithTag("Coin");
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;
    }

    // Update is called once per frame
    // void Update()
    // {
    // }

    // FixedUpdate is called once every physic update
    void FixedUpdate()
    {
        rigidBodyComponent.linearVelocity = new Vector3(horizontalInput, rigidBodyComponent.linearVelocity.y, 0f);

        // Debug.Log(rigidBodyComponent.position.y);

        if (rigidBodyComponent.position.y <= -4)
        {
            // rigidBodyComponent.MovePosition(new Vector3(0, 2, 0));
            rigidBodyComponent.position = new Vector3(0, 2, 0);
            rigidBodyComponent.linearVelocity = Vector3.zero; 
            // Instantiate(coin, new Vector3(2.55f, 1.37f, 0.18f), Quaternion.identity);
        }
        
        // isGrounded = Physics.OverlapSphere(groundCheckTransform.position, 0.2f).Length > 1;
        // Debug.Log(isGrounded);
        // isGrounded = Physics.CheckSphere(groundCheckTransform.position, 0.1f, LayerMask.GetMask("Ground"));
    }
    
}
