using BNG;
using UnityEngine;

public class DraggableGroup : MonoBehaviour
{
    [SerializeField] private GameObject ground;
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private HandController leftHand;
    [SerializeField] private HandController rightHand;
    [SerializeField] private Collider[] objectsToCollideWith;
    
    public Collider PlayerCollider => playerCollider;
    public Rigidbody PlayerRb => playerRb;
    public GameObject Ground => ground;
    
    public HandController LeftHand => leftHand;
    public HandController RightHand => rightHand;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start()
    // {
    //     
    // }
    //
    // // Update is called once per frame
    // void Update()
    // {
    //     
    // }
    
    
    
}
