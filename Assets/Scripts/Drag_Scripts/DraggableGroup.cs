using UnityEngine;

public class DraggableGroup : MonoBehaviour
{
    [SerializeField] private GameObject ground;
    [Tooltip("Left empty, the player rig in the scene fills these in at runtime.")]
    [SerializeField] private Collider playerCollider;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private Collider[] objectsToCollideWith;
    
    public Collider PlayerCollider => playerCollider;
    public GameObject Ground => ground;
    
    public Transform LeftHand => leftHand;
    public Transform RightHand => rightHand;

    /// <summary>
    /// Take whatever was not wired in the Inspector from the rig in the scene, so swapping
    /// the player prefab does not leave these fields pointing at a deleted rig.
    /// </summary>
    private void Awake()
    {
        PlayerRig rig = PlayerRig.Instance;
        if (rig == null) return;

        if (playerCollider == null) playerCollider = rig.BodyCollider;
        if (leftHand == null) leftHand = rig.LeftHand;
        if (rightHand == null) rightHand = rig.RightHand;
    }
    
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
